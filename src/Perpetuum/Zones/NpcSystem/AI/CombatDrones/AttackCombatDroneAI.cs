using Perpetuum.Zones.NpcSystem.TargettingStrategies;
using System;

namespace Perpetuum.Zones.NpcSystem.AI.CombatDrones
{
    public class AttackCombatDroneAI : CombatAI
    {
        public AttackCombatDroneAI(SmartCreature smartCreature) : base(smartCreature) { }

        protected override CombatPrimaryLockSelectionStrategySelector InitSelector()
        {
            return CombatPrimaryLockSelectionStrategySelector.Create()
                .WithStrategy(CombatPrimaryLockSelectionStrategy.HostileOrClosest, 1)
                .Build();
        }

        public override void Exit()
        {
            this.source?.Cancel();

            base.Exit();
        }

        public override void Update(TimeSpan time)
        {
            if (!smartCreature.ThreatManager.IsThreatened)
            {
                ReturnToHomePosition();

                return;
            }

            this.UpdateHostile(time, false);

            base.Update(time);
        }

        protected override void ToAggressorAI() { }

        protected override void ReturnToHomePosition()
        {
            smartCreature.AI.Pop();
            smartCreature.AI.Push(new EscortCombatDroneAI(smartCreature));
            this.WriteLog("Returning to command robot.");
        }
    }
}
