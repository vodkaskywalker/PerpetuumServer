using System;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class SmartCreatureStationaryIdleAI : SmartCreatureAI
    {
        public SmartCreatureStationaryIdleAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Enter()
        {
            this.smartCreature.StopAllModules();
            this.smartCreature.ResetLocks();
            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            if (this.smartCreature.ThreatManager.IsThreatened)
            {
                ToAggressorAI();
            }
        }

        protected override void ToHomeAI() { }

        protected override void ToAggressorAI()
        {
            if (this.smartCreature.Behavior.Type == SmartCreatureBehaviorType.Passive)
            {
                return;
            }

            this.smartCreature.AI.Push(new SmartCreatureStationaryCombatAI(this.smartCreature));
        }
    }
}
