using Perpetuum.Timers;
using Perpetuum.Zones.NpcSystem.TargettingStrategies;
using System;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class StationaryIndustrialAI : IndustrialAI
    {
        public readonly IntervalTimer UpdateFrequency = new IntervalTimer(5000);

        public StationaryIndustrialAI(SmartCreature smartCreature) : base(smartCreature) { }

        protected override IndustrialPrimaryLockSelectionStrategySelector InitSelector()
        {
            return IndustrialPrimaryLockSelectionStrategySelector.Create()
                .WithStrategy(IndustrialPrimaryLockSelectionStrategy.RichestTile, 1)
                .Build();
        }

        protected override TimeSpan SetPrimaryDwellTime()
        {
            return FastRandom.NextTimeSpan(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
        }
    }
}
