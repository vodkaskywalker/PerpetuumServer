using Perpetuum.Zones.Movements;
using Perpetuum.Zones.RemoteControl;
using System;

namespace Perpetuum.Zones.NpcSystem.AI.CombatDrones
{
    public class GuardCombatDroneAI : BaseAI
    {
        private RandomMovement movement;

        public GuardCombatDroneAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Enter()
        {
            this.smartCreature.StopAllModules();
            this.smartCreature.ResetLocks();

            this.movement = new RandomMovement(this.smartCreature.HomePosition, (this.smartCreature as CombatDrone).GuardRange);

            movement.Start(this.smartCreature);

            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            if (!(this.smartCreature as CombatDrone).IsInGuardRange)
            {
                this.ToEscortCombatDroneAI();

                return;
            }

            if (this.smartCreature.ThreatManager.IsThreatened)
            {
                this.ToAttackCombatDroneAI();

                return;
            }

            this.movement?.Update(this.smartCreature, time);
        }
    }
}
