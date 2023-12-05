using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Zones.Terrains.Materials.Plants.Harvesters;

namespace Perpetuum.Modules
{
    public class RemoteControlledHarvesterModule : HarvesterModule
    {
        private const int MAX_EP_PER_DAY = 1440;
        private readonly PlantHarvester.Factory _plantHarvesterFactory;
        private readonly HarvestingAmountModifierProperty _harverstingAmountModifier;

        public RemoteControlledHarvesterModule(PlantHarvester.Factory plantHarvesterFactory) : base(CategoryFlags.undefined, plantHarvesterFactory)
        {
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
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
    }
}
