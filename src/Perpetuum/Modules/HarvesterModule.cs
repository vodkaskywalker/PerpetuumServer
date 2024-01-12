using System;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants.Harvesters;

namespace Perpetuum.Modules
{
    public class HarvesterModule : GathererModule
    {
        private const int MAX_EP_PER_DAY = 1440;
        private readonly PlantHarvester.Factory _plantHarvesterFactory;
        private readonly HarvestingAmountModifierProperty _harverstingAmountModifier;

        public HarvesterModule(CategoryFlags ammoCategoryFlags,PlantHarvester.Factory plantHarvesterFactory) : base(ammoCategoryFlags, true)
        {
            _plantHarvesterFactory = plantHarvesterFactory;
            _harverstingAmountModifier = new HarvestingAmountModifierProperty(this);
            AddProperty(_harverstingAmountModifier);
            cycleTime.AddEffectModifier(AggregateField.effect_harvesting_cycle_time_modifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override void UpdateProperty(AggregateField field)
        {
            base.UpdateProperty(field);

            switch (field)
            {
                case AggregateField.harvesting_amount_modifier:
                case AggregateField.effect_harvesting_amount_modifier:
                    {
                        _harverstingAmountModifier.Update();

                        break;
                    }
            }
        }

        protected override int CalculateEp(int materialType)
        {

            var activeGathererModules = this is RemoteControlledHarvesterModule
                ? ParentRobot.ActiveModules.OfType<RemoteControlledHarvesterModule>().Where(m => m.State.Type != ModuleStateType.Idle).ToArray()
                : ParentRobot.ActiveModules.OfType<HarvesterModule>().Where(m => m.State.Type != ModuleStateType.Idle).ToArray();

            if (activeGathererModules.Length == 0)
            {
                return 0;
            }

            var avgCycleTime = activeGathererModules.Select(m => m.CycleTime).Average();
            var t = TimeSpan.FromDays(1).Divide(avgCycleTime);
            var chance = (double)MAX_EP_PER_DAY / t.Ticks;

            chance /= activeGathererModules.Length;

            var rand = FastRandom.NextDouble();

            if (rand <= chance)
            {
                return 1;
            }

            return 0;
        }

        protected override void OnAction()
        {
            var zone = Zone;

            if (zone == null)
            {
                return;
            }

            DoHarvesting(zone);
            ConsumeAmmo();
        }

        public void DoHarvesting(IZone zone)
        {
            var terrainLock = GetLock().ThrowIfNotType<TerrainLock>(ErrorCodes.InvalidLockType);

            CreateBeam(terrainLock.Location, BeamState.AlignToTerrain);

            using (var scope = Db.CreateTransaction())
            {
                using (new TerrainUpdateMonitor(zone))
                {
                    var plantInfo = zone.Terrain.Plants.GetValue(terrainLock.Location);
                    var amountModifier = _harverstingAmountModifier.GetValueByPlantType(plantInfo.type);

                    IPlantHarvester plantHarvester = _plantHarvesterFactory(zone, amountModifier);

                    var harvestedPlants = plantHarvester.HarvestPlant(terrainLock.Location);

                    Debug.Assert(ParentRobot != null, "ParentRobot != null");

                    var container = ParentRobot.GetContainer();

                    Debug.Assert(container != null, "container != null");
                    container.EnlistTransaction();

                    var player = ParentRobot is RemoteControlledCreature
                        ? (ParentRobot as RemoteControlledCreature).Player
                        : ParentRobot as Player;

                    Debug.Assert(player != null,"player != null");

                    foreach (var extractedMaterial in harvestedPlants)
                    {
                        var item = (Item)Factory.CreateWithRandomEID(extractedMaterial.Definition);

                        item.Owner = Owner;
                        item.Quantity = extractedMaterial.Quantity;
                        container.AddItem(item, true);

                        var extractedHarvestDefinition = extractedMaterial.Definition;
                        var extractedQuantity = extractedMaterial.Quantity;

                        player.MissionHandler.EnqueueMissionEventInfo(new HarvestPlantEventInfo(player, extractedHarvestDefinition, extractedQuantity, terrainLock.Location));
                        player.Zone?.HarvestLogHandler.EnqueueHarvestLog(extractedHarvestDefinition, extractedQuantity);
                    }

                    container.Save();
                    OnGathererMaterial(zone, player, (int) plantInfo.type);
                    Transaction.Current.OnCommited(() => container.SendUpdateToOwnerAsync());
                    scope.Complete();
                }
            }
        }
    }
}