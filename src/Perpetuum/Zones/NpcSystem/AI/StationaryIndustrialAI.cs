using Perpetuum.Timers;
using System;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class StationaryIndustrialAI : IndustrialAI
    {
        private readonly IntervalTimer updateFrequency = new IntervalTimer(18000);

        public StationaryIndustrialAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Update(TimeSpan time)
        {
            FindIndustrialTargets(time);

            base.Update(time);
        }

        protected override TimeSpan SetPrimaryDwellTime()
        {
            return FastRandom.NextTimeSpan(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
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
    }
}
