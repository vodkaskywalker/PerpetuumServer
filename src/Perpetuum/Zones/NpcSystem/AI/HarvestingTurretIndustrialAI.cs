using Perpetuum.Zones.NpcSystem.TargettingStrategies;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class HarvestingTurretIndustrialAI : StationaryIndustrialAI
    {
        public HarvestingTurretIndustrialAI(SmartCreature smartCreature) : base(smartCreature) { }

        protected override IndustrialPrimaryLockSelectionStrategySelector InitSelector()
        {
            return IndustrialPrimaryLockSelectionStrategySelector.Create()
                .WithStrategy(IndustrialPrimaryLockSelectionStrategy.MostFertilePlantWithinOptimal, 1)
                .Build();
        }
    }
}
