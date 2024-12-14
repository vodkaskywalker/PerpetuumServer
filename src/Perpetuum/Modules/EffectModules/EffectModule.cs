﻿using Perpetuum.EntityFramework;
using Perpetuum.StateMachines;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Modules.EffectModules
{
    public abstract class EffectModule : ActiveModule
    {
        private readonly EffectToken token = EffectToken.NewToken();

        protected EffectModule() : this(false)
        {

        }

        protected EffectModule(bool ranged) : base(ranged)
        {
        }

        protected override void OnStateChanged(IState state)
        {
            IModuleState moduleState = (IModuleState)state;

            if (moduleState.Type == ModuleStateType.Idle)
            {
                if (ED.AttributeFlags.SelfEffect)
                {
                    ParentRobot?.EffectHandler.RemoveEffectByToken(token);
                }
            }

            base.OnStateChanged(state);
        }

        protected override void OnAction()
        {
            Unit target;

            if (ED.AttributeFlags.SelfEffect)
            {
                target = ParentRobot;
            }
            else
            {
                UnitLock unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);
                target = unitLock.Target;
            }

            if (!CanApplyEffect(target))
            {
                return;
            }

            target.InZone.ThrowIfFalse(ErrorCodes.TargetNotFound);
            target.States.Dead.ThrowIfTrue(ErrorCodes.TargetIsDead);

            OnApplyingEffect(target);

            EffectBuilder effectBuilder = target.NewEffectBuilder();
            SetupEffect(effectBuilder);
            effectBuilder.WithToken(token);
            target.ApplyEffect(effectBuilder);
        }

        protected virtual bool CanApplyEffect(Unit target)
        {
            return true;
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        protected virtual void OnApplyingEffect(Unit target) { }

        protected abstract void SetupEffect(EffectBuilder effectBuilder);
    }
}