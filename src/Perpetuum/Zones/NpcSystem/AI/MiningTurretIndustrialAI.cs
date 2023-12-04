using Perpetuum.Zones.NpcSystem.TargettingStrategies;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class MiningTurretIndustrialAI : StationaryIndustrialAI
    {
        public MiningTurretIndustrialAI(SmartCreature smartCreature) : base(smartCreature) { }

        protected override IndustrialPrimaryLockSelectionStrategySelector InitSelector()
        {
            return IndustrialPrimaryLockSelectionStrategySelector.Create()
                .WithStrategy(IndustrialPrimaryLockSelectionStrategy.RichestTileWithinOptimal, 1)
                .Build();
        }
    }
}
