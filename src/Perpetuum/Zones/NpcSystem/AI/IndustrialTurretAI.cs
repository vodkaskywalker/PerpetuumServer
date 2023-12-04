using Perpetuum.Timers;
using Perpetuum.Zones.NpcSystem.TargettingStrategies;
using System;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class IndustrialTurretAI : IndustrialAI
    {
        private readonly IntervalTimer updateFrequency = new IntervalTimer(2000);

        public IndustrialTurretAI(SmartCreature smartCreature) : base(smartCreature) { }

        protected override IndustrialPrimaryLockSelectionStrategySelector InitSelector()
        {
            return IndustrialPrimaryLockSelectionStrategySelector.Create()
                .WithStrategy(IndustrialPrimaryLockSelectionStrategy.RichestTileWithinOptimal, 10)
                .Build();
        }

        public override void Update(TimeSpan time)
        {
            FindIndustrialTargets(time);

            base.Update(time);
        }

        private void FindIndustrialTargets(TimeSpan time)
        {
            updateFrequency.Update(time);

            if (updateFrequency.Passed)
            {
                updateFrequency.Reset();
                smartCreature.LookingForIndustrialTargets();
            }
        }

        protected override TimeSpan SetPrimaryDwellTime()
        {
            return FastRandom.NextTimeSpan(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
        }
    }
}
