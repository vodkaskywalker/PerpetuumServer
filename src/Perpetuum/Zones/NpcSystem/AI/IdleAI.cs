using Perpetuum.Zones.Movements;
using System;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class IdleAI : BaseAI
    {
        private RandomMovement movement;

        public IdleAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Enter()
        {
            this.smartCreature.StopAllModules();
            this.smartCreature.ResetLocks();

            this.movement = new RandomMovement(this.smartCreature.HomePosition, this.smartCreature.HomeRange);

            movement.Start(this.smartCreature);

            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            if (!this.smartCreature.IsInHomeRange)
            {
                this.ToHomeAI();

                return;
            }

            if (this.smartCreature.ThreatManager.IsThreatened)
            {
                this.ToAggressorAI();

                return;
            }

            this.movement?.Update(this.smartCreature, time);
        }
    }
}
