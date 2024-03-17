using Perpetuum.Log;
using Perpetuum.StateMachines;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.NpcSystem.AI.CombatDrones;
using System;
using System.Diagnostics;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public abstract class BaseAI : IState
    {
        protected readonly SmartCreature smartCreature;

        protected BaseAI(SmartCreature smartCreature)
        {
            this.smartCreature = smartCreature;
        }

        public virtual void Enter()
        {
            WriteLog("enter state = " + GetType().Name);
        }

        public virtual void Exit()
        {
            smartCreature.StopMoving();
            WriteLog("exit state = " + GetType().Name);
        }

        public abstract void Update(TimeSpan time);

        protected virtual void ToHomeAI()
        {
            this.smartCreature.AI.Push(new HomingAI(smartCreature));
        }

        protected virtual void ToAggressorAI()
        {
            if (this.smartCreature.Behavior.Type == BehaviorType.Passive)
            {
                return;
            }

            this.smartCreature.AI.Push(new AggressorAI(smartCreature));
        }

        protected virtual void ToAttackCombatDroneAI()
        {
            this.smartCreature.AI.Push(new AttackCombatDroneAI(smartCreature));
        }

        [Conditional("DEBUG")]
        protected void WriteLog(string message)
        {
            Logger.DebugInfo($"SmartCreatureAI: {message}");
        }

        protected virtual void ToEscortCombatDroneAI()
        {
            this.smartCreature.AI.Push(new EscortCombatDroneAI(smartCreature));
        }
    }
}
