using Perpetuum.Timers;
using System;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class StationaryCombatAI : CombatAI
    {
        private readonly IntervalTimer updateFrequency = new IntervalTimer(650);

        public StationaryCombatAI(SmartCreature smartCreature) : base(smartCreature) { }

        protected override PrimaryLockSelectionStrategySelector InitSelector()
        {
            return PrimaryLockSelectionStrategySelector.Create()
                .WithStrategy(PrimaryLockSelectionStrategy.Hostile, 1)
                .WithStrategy(PrimaryLockSelectionStrategy.Closest, 2)
                .WithStrategy(PrimaryLockSelectionStrategy.OptimalRange, 3)
                .WithStrategy(PrimaryLockSelectionStrategy.Random, 10)
                .Build();
        }

        public override void Update(TimeSpan time)
        {
            FindHostiles(time);

            base.Update(time);
        }

        private void FindHostiles(TimeSpan time)
        {
            updateFrequency.Update(time);

            if (updateFrequency.Passed)
            {
                updateFrequency.Reset();
                smartCreature.LookingForHostiles();
            }
        }

        protected override TimeSpan SetPrimaryDwellTime()
        {
            return FastRandom.NextTimeSpan(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
        }
    }
}
