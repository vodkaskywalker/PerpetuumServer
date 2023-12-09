﻿using System;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class HarvestingIndustrialTurretAI : StationaryIndustrialAI
    {
        public HarvestingIndustrialTurretAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Update(TimeSpan time)
        {
            FindIndustrialTargets(time);

            base.Update(time);
        }

        public void FindIndustrialTargets(TimeSpan time)
        {
            UpdateFrequency.Update(time);

            if (UpdateFrequency.Passed)
            {
                UpdateFrequency.Reset();
                smartCreature.LookingForHarvestingTargets();
            }
        }
    }
}