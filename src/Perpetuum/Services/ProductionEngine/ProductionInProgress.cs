using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Items.Helpers;
using Perpetuum.Log;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Services.ProductionEngine
{
    public static class ProductionInProgressExtensions
    {
        public static IDictionary<string, object> ToDictionary(this IEnumerable<ProductionInProgress> productions)
        {
            return productions.ToDictionary("c", p => p.ToDictionary());
        }
        
        public static IEnumerable<ProductionInProgress> GetByCorporation(this IEnumerable<ProductionInProgress> productions,Corporation corporation)
        {
            return corporation.GetCharacterMembers().SelectMany(productions.GetCorporationPaidProductionsByCharacter);
        }

        public static IEnumerable<ProductionInProgress> GetCorporationPaidProductionsByFacililtyAndCharacter(this IEnumerable<ProductionInProgress> productions,Character character, long facilityEid)
        {
            return productions.GetRunningProductionsByFacilityAndCharacter(character, facilityEid).Where(pip => pip.UseCorporationWallet);
        }

        public static IEnumerable<ProductionInProgress> GetRunningProductionsByFacilityAndCharacter(this IEnumerable<ProductionInProgress> productions, Character character, long facilityEid)
        {
            return productions.GetByCharacter(character).Where(pip => pip.FacilityEID == facilityEid);
        }

        public static IEnumerable<ProductionInProgress> GetCorporationPaidProductionsByCharacter(this IEnumerable<ProductionInProgress> productions,Character character)
        {
            return productions.GetByCharacter(character).Where(pip => pip.UseCorporationWallet);
        }
        
        public static IEnumerable<ProductionInProgress> GetByCharacter(this IEnumerable<ProductionInProgress> productions, Character character)
        {
            return productions.Where(pip => pip.Character == character);
        }
    }

    public interface IProductionInProgressRepository
    {
        void Delete(ProductionInProgress productionInProgress);

        IEnumerable<ProductionInProgress> GetAllByFacility(ProductionFacility facility);
        IEnumerable<ProductionInProgress> GetAll();
    }

    public class ProductionInProgressRepository : IProductionInProgressRepository
    {
        private readonly ProductionInProgress.Factory _pipFactory;

        public ProductionInProgressRepository(ProductionInProgress.Factory pipFactory)
        {
            _pipFactory = pipFactory;
        }

        public void Delete(ProductionInProgress pip)
        {
            Db.Query().CommandText("delete runningproductionreserveditem where runningid=@ID")
                .SetParameter("@ID",pip.ID)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);

            Db.Query().CommandText("delete runningproduction where ID=@ID")
                .SetParameter("@ID", pip.ID)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);
        }

        public IEnumerable<ProductionInProgress> GetAllByFacility(ProductionFacility facility)
        {
            return Db.Query().CommandText("select * from runningproduction where facilityEID = @facilityEID")
                           .SetParameter("@facilityEID",facility.Eid)
                           .Execute()
                           .Select(CreateProductionInProgressFromRecord)
                           .ToArray();
        }

        public IEnumerable<ProductionInProgress> GetAll()
        {
            return Db.Query().CommandText("select * from runningproduction").Execute().Select(CreateProductionInProgressFromRecord).ToArray();
        }

        private ProductionInProgress CreateProductionInProgressFromRecord(IDataRecord record)
        {
            var pip = _pipFactory();
            pip.ID = record.GetValue<int>("id");
            pip.Character = Character.Get(record.GetValue<int>("characterID"));
            pip.ResultDefinition = record.GetValue<int>("resultDefinition");
            pip.Type = (ProductionInProgressType) record.GetValue<int>("type");
            pip.StartTime = record.GetValue<DateTime>("startTime");
            pip.FinishTime = record.GetValue<DateTime>("finishTime");
            pip.FacilityEID = record.GetValue<long>("facilityEID");
            pip.TotalProductionTimeSeconds = record.GetValue<int>("totalProductionTime");
            pip.BaseEID = record.GetValue<long>("baseEID");
            pip.CreditTaken = record.GetValue<double>("creditTaken");
            pip.PricePerSecond = record.GetValue<double>("pricePerSecond");
            pip.UseCorporationWallet = record.GetValue<bool>("useCorporationWallet");
            pip.AmountOfCycles = record.GetValue<int>("amountOfCycles");
            pip.Paused = record.GetValue<bool>("paused");
            pip.PauseTime = record.GetValue<DateTime?>("pausetime");
            return pip;
        }
    }

    public class ProductionInProgress
    {
        private readonly ItemHelper itemHelper;

        private readonly DockingBaseHelper dockingBaseHelper;

        public int ID { get; set; }

        public Character Character { get; set; }

        public int ResultDefinition { get; set; }

        public ProductionInProgressType Type { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime FinishTime { get; set; }

        public long FacilityEID { get; set; }

        public int TotalProductionTimeSeconds { get; set; }

        public long BaseEID { get; set; }

        public double CreditTaken { get; set; }

        public double PricePerSecond { get; set; }

        public int AmountOfCycles { get; set; }

        public bool UseCorporationWallet { get; set; }

        public bool Paused { get; set; }

        public DateTime? PauseTime { get; set; }

        public long[] ReservedEids { get; set; }

        public TimeSpan TotalProductionTime => TimeSpan.FromSeconds(TotalProductionTimeSeconds);

        public delegate ProductionInProgress Factory();

        public ProductionInProgress(ItemHelper itemHelper,DockingBaseHelper dockingBaseHelper)
        {
            this.itemHelper = itemHelper;
            this.dockingBaseHelper = dockingBaseHelper;
        }

        public void WriteLog()
        {
            ProductionHelper.ProductionLogInsert(Character, ResultDefinition, GetResultingAmount(), Type, CalculateDurationSeconds(), Price, UseCorporationWallet);
        }

        public IEnumerable<Item> GetReservedItems()
        {
            foreach (var reservedEid in ReservedEids)
            {
                var item = itemHelper.LoadItem(reservedEid);
                if (item != null)
                    yield return item;
            }
        }

        public void LoadReservedItems()
        {
            ReservedEids =
                (from r in
                     Db.Query().CommandText("select reservedEID from runningproductionreserveditem where runningID=@ID").SetParameter("@ID", ID)
                     .Execute()
                 select r.GetValue<long>(0)).ToArray();
        }

        private int CalculateDurationSeconds()
        {
            return (int)FinishTime.Subtract(StartTime).TotalSeconds;
        }


        private int GetResultingAmount()
        {
            if (!EntityDefault.TryGet(ResultDefinition, out EntityDefault ed))
            {
                Logger.Error("consistency error! definition was not found for productioninprogress withdrawcredit. definition: " + ResultDefinition);
            }

            var resultingAmount = AmountOfCycles * ed.Quantity;

            return resultingAmount;
        }


        private double Price
        {
            get
            {
                if (IsMissionRelated)
                {
                    return 0;
                }

                switch (Type)
                {

                    case ProductionInProgressType.massProduction:
                        return TotalProductionTimeSeconds  * PricePerSecond;

                    default:
                        return TotalProductionTimeSeconds  * PricePerSecond;
                }
            }
        }

        public bool IsMissionRelated
        {
            get
            {
                var resultEd = EntityDefault.Get(ResultDefinition);

                if (resultEd.CategoryFlags.IsCategory(CategoryFlags.cf_random_items) ||
                    resultEd.CategoryFlags.IsCategory(CategoryFlags.cf_random_calibration_programs))
                {
                    return true;
                }

                return false;
            }
        }


        public Dictionary<string, object> ToDictionary()
        {
            var resultQuantity = AmountOfCycles;
            EntityDefault resultEd;
            if (EntityDefault.TryGet(ResultDefinition, out resultEd))
            {
                resultQuantity *= resultEd.Quantity;
            }

            var tmpDict = new Dictionary<string, object>
                              {
                                  {k.definition, ResultDefinition},
                                  {k.startTime, StartTime},
                                  {k.finishTime, FinishTime},
                                  {k.ID, ID},
                                  {k.facility, FacilityEID},
                                  {k.productionTime, TotalProductionTimeSeconds},
                                  {k.timeLeft, (int) FinishTime.Subtract(DateTime.Now).TotalSeconds},
                                  {k.type, (int) Type},
                                  {k.baseEID, BaseEID},
                                  {k.cycle, resultQuantity},
                                  {k.price, Price},
                                  {k.characterID, Character.Id},
                                  {k.useCorporationWallet, UseCorporationWallet},
                                  {k.paused, Paused},
                                  {k.pauseTime, PauseTime},
                              };

            return tmpDict;

        }

        public override string ToString()
        {
            return string.Format("ID:{0} characterID:{1} characterEID:{2} resultDefinition:{3} {9} type:{4} facilityEID:{5} baseEID:{6} price:{7} amountOfCycles:{8}", ID, Character.Id, Character.Eid, ResultDefinition, Type, FacilityEID, BaseEID, Price, AmountOfCycles, EntityDefault.Get(ResultDefinition).Name);
        }



        /// <summary>
        /// take credit for the production from the character
        /// </summary>
        public bool TryWithdrawCredit()
        {
            Logger.Info("withdrawing credit for: " + this);

            var transactionType = TransactionType.ProductionManufacture;
            switch (Type)
            {
                case ProductionInProgressType.licenseCreate:
                case ProductionInProgressType.manufacture:
                case ProductionInProgressType.patentMaterialEfficiencyDevelop:
                case ProductionInProgressType.patentNofRunsDevelop:
                case ProductionInProgressType.patentTimeEfficiencyDevelop:
                {
                    Logger.Error("consistency error! outdated production type. " + Type);
                    throw new PerpetuumException(ErrorCodes.ServerError);
                }

                case ProductionInProgressType.research:
                    transactionType = TransactionType.ProductionResearch;
                    break;
                case ProductionInProgressType.prototype:
                    transactionType = TransactionType.ProductionPrototype;
                    break;
                case ProductionInProgressType.massProduction:
                    transactionType = TransactionType.ProductionMassProduction;
                    break;

                case ProductionInProgressType.calibrationProgramForge:
                    transactionType = TransactionType.ProductionCPRGForge;
                    break;
            }

            //no price, no process
            if (Math.Abs(Price) < double.Epsilon)
            {
                Logger.Info("price is 0 for " + this);
                return true;
            }

            //take his money
            CreditTaken = Price; //safety for the cancel
            
            var wallet = Character.GetWallet(UseCorporationWallet,transactionType);

            if (wallet.Balance < Price)
                return false;

            wallet.Balance -= Price;

            var b = TransactionLogEvent.Builder()
                                       .SetTransactionType(transactionType)
                                       .SetCreditBalance(wallet.Balance)
                                       .SetCreditChange(-Price)
                                       .SetCharacter(Character)
                                       .SetItem(ResultDefinition, GetResultingAmount());
            var corpWallet = wallet as CorporationWallet;
            if (corpWallet != null)
            {
                b.SetCorporation(corpWallet.Corporation);
                corpWallet.Corporation.LogTransaction(b);
            }
            else
            {
                Character.LogTransaction(b);
            }

            dockingBaseHelper.GetDockingBase(BaseEID).AddCentralBank(transactionType, Price);
            return true;
        }

        public void SetPause(bool isPaused)
        {
            if (isPaused)
            {
                if (!Paused)
                {
                    // >> to paused state
                    PauseProduction();
                }
            }
            else
            {
                if (Paused)
                {
                    // >> to unpause state
                    ResumeProduction();
                    
                }
            }
        }

        private void ResumeProduction()
        {
            if (!Paused)
            {
                Logger.Warning("production already running: " + this);
                return;
            }

            Paused = false;

            if (PauseTime == null)
            {
                Logger.Error("wtf pause time is null. " + this);
                return;
            }

            var pauseMoment = (DateTime) PauseTime;

            var doneBackThen = pauseMoment.Subtract(StartTime);

            StartTime = DateTime.Now.Subtract(doneBackThen);
            FinishTime = StartTime.AddSeconds(TotalProductionTimeSeconds);

            var res =
                Db.Query().CommandText("update runningproduction set starttime=@startTime,finishtime=@finishTime,paused=0,pauseTime=null where id=@id").SetParameter("@startTime", StartTime).SetParameter("@finishTime", FinishTime).SetParameter("@id", ID)
                    .ExecuteNonQuery();

            if (res != 1)
            {
                Logger.Error("error updating the resumed production. " + this);
            }
        }

        private void PauseProduction()
        {
            if (Paused)
            {
                Logger.Warning("production already paused: " + this);
                return;
            }

            Paused = true;
            PauseTime = DateTime.Now;

            var res=
            Db.Query().CommandText("update runningproduction set paused=1,pausetime=@pauseTime where id=@id").SetParameter("@pauseTime", PauseTime).SetParameter("@id", ID)
                .ExecuteNonQuery();

            if (res != 1)
            {
                Logger.Error("error updating the paused production. " + this);
            }
        }

        public ErrorCodes HasAccess(Character issuerCharacter)
        {
            return Character == issuerCharacter ? ErrorCodes.NoError : ErrorCodes.AccessDenied;
        }

        /// <summary>
        /// This command refreshes the corporation production info
        /// Technically corp members with proper roles see their corpmate's production events. start/end/cancel
        /// </summary>
        /// <param name="command"></param>
        private void SendProductionEventToCorporationMembers(Command command)
        {
            if (!UseCorporationWallet)
                return;

            var replyDict = new Dictionary<string, object> {{k.production, ToDictionary()}};

            const CorporationRole roleMask = CorporationRole.CEO | CorporationRole.DeputyCEO | CorporationRole.ProductionManager | CorporationRole.Accountant;
            Message.Builder.SetCommand(command)
                .WithData(replyDict)
                .ToCorporation(Character.CorporationEid, roleMask)
                .Send();
        }

        public void SendProductionEventToCorporationMembersOnCommitted(Command command)
        {
            Transaction.Current.OnCommited(()=> SendProductionEventToCorporationMembers(command));
        }

        public void InsertProductionInProgess()
        {
            //insert new running production
            var id = Db.Query().CommandText(@"insert runningproduction (characterID,characterEID,resultDefinition,type,startTime,finishTime,facilityEID,totalProductionTime,baseEID,creditTaken,pricePerSecond,amountofcycles,useCorporationWallet) values
								(@characterID,@characterEID,@resultDefinition,@type,@startTime,@finishTime,@facilityEID,@totalProductionTime,@baseEID,@creditTaken,@pricePerSecond,@amountOfCycles,@useCorporationWallet);
								select cast(scope_identity() as int)")
                .SetParameter("@characterID", Character.Id)
                .SetParameter("@characterEID", Character.Eid)
                .SetParameter("@resultDefinition", ResultDefinition)
                .SetParameter("@type", (int) Type)
                .SetParameter("@startTime", StartTime)
                .SetParameter("@finishTime", FinishTime)
                .SetParameter("@facilityEID", FacilityEID)
                .SetParameter("@totalProductionTime", TotalProductionTimeSeconds)
                .SetParameter("@baseEID", BaseEID)
                .SetParameter("@creditTaken", CreditTaken)
                .SetParameter("@pricePerSecond", PricePerSecond)
                .SetParameter("@amountOfCycles", AmountOfCycles)
                .SetParameter("@useCorporationWallet", UseCorporationWallet)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            //save reserved EIDs
            foreach (var eid in ReservedEids)
            {
                Db.Query().CommandText("insert runningproductionreserveditem (runningid, reservedeid) values (@runningID, @reservedEID)")
                    .SetParameter("@runningID", id)
                    .SetParameter("@reservedEID", eid)
                    .ExecuteNonQuery();
            }

            //store ID
            ID = id;
        }
    }
}
