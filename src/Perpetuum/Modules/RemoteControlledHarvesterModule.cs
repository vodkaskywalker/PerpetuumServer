using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Players;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains.Materials.Plants.Harvesters;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones;
using System.Diagnostics;
using System.Transactions;

namespace Perpetuum.Modules
{
    public class RemoteControlledHarvesterModule : GathererModule
    {
        private readonly PlantHarvester.Factory _plantHarvesterFactory;
        private readonly HarvestingAmountModifierProperty _harverstingAmountModifier;

        public RemoteControlledHarvesterModule(PlantHarvester.Factory plantHarvesterFactory) : base(CategoryFlags.undefined, true)
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

        protected override void OnAction()
        {
            var zone = Zone;

            if (zone == null)
            {
                return;
            }

            DoHarvesting(zone);
        }

        protected override int CalculateEp(int materialType)
        {
            return 0;
        }

        private void DoHarvesting(IZone zone)
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

                    foreach (var extractedMaterial in harvestedPlants)
                    {
                        var item = (Item)Factory.CreateWithRandomEID(extractedMaterial.Definition);

                        item.Owner = Owner;
                        item.Quantity = extractedMaterial.Quantity;
                        container.AddItem(item, true);
                    }

                    container.Save();
                    Transaction.Current.OnCommited(() => container.SendUpdateToOwnerAsync());
                    scope.Complete();
                }
            }
        }
    }
}
