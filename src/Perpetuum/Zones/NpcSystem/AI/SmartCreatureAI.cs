using Perpetuum.Log;
using Perpetuum.StateMachines;
using System;
using System.Diagnostics;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public abstract class SmartCreatureAI : IState
    {
        protected readonly SmartCreature smartCreature;

        protected SmartCreatureAI(SmartCreature smartCreature)
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
            this.smartCreature.AI.Push(new SmartCreatureHomingAI(smartCreature));
        }

        protected virtual void ToAggressorAI()
        {
            if (this.smartCreature.Behavior.Type == SmartCreatureBehaviorType.Passive)
            {
                return;
            }

            this.smartCreature.AI.Push(new SmartCreatureAggressorAI(smartCreature));
        }

        [Conditional("DEBUG")]
        protected void WriteLog(string message)
        {
            Logger.DebugInfo($"SmartCreatureAI: {message}");
        }
    }
}
