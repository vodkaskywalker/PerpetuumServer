﻿using System;

namespace Perpetuum.Zones.DamageProcessors
{
    public class CombatEventArgs : EventArgs
    {
    }

    public class DamageTakenEventArgs : CombatEventArgs
    {
        public double TotalDamage { get; set; }

        public double TotalCoreDamage { get; set; }

        public double TotalKers { get; set; }

        public double TotalEnergyDamage { get; set; }

        public double TotalSpeedDamage { get; set; }

        public double TotalAcidDamage { get; set; }

        public bool IsCritical { get; set; }

        public bool IsKillingBlow { get; set; }
    }

    public class DemobilizerEventArgs : CombatEventArgs
    {
    }

    public class SensorDampenerEventArgs : CombatEventArgs
    {
    }

    public class KillingBlowEventArgs : CombatEventArgs
    {
    }
}