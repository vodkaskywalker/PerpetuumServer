using Perpetuum.Timers;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.NpcSystem.TargettingStrategies;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Zones.NpcSystem.IndustrialTargetsManagement;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class IndustrialAI : BaseAI
    {
        private const int UpdateFrequency = 1650;
        private readonly IntervalTimer processIndustrialTargetsTimer = new IntervalTimer(UpdateFrequency);
        private IntervalTimer primarySelectTimer;
        private List<ModuleActivator> moduleActivators;
        private TimeSpan industrialTargetsUpdateFrequency = TimeSpan.FromMilliseconds(UpdateFrequency);
        private IndustrialPrimaryLockSelectionStrategySelector stratSelector;

        public IndustrialAI(SmartCreature smartCreature) : base(smartCreature)
        {
            primarySelectTimer = new IntervalTimer((this.smartCreature.ActiveModules.Max(x => x?.CycleTime.Milliseconds) ?? 0) + UpdateFrequency);
        }

        public override void Enter()
        {
            stratSelector = InitSelector();
            moduleActivators = this.smartCreature.ActiveModules
                .Select(m => new ModuleActivator(m))
                .ToList();
            processIndustrialTargetsTimer.Update(industrialTargetsUpdateFrequency);
            primarySelectTimer.Update(industrialTargetsUpdateFrequency);

            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            UpdateIndustrialTargets(time);
            UpdatePrimaryTarget(time);
            RunModules(time);
        }

        protected virtual IndustrialPrimaryLockSelectionStrategySelector InitSelector()
        {
            return IndustrialPrimaryLockSelectionStrategySelector.Create()
                .WithStrategy(IndustrialPrimaryLockSelectionStrategy.RichestTile, 9)
                .Build();
        }

        protected void UpdateIndustrialTargets(TimeSpan time)
        {
            processIndustrialTargetsTimer.Update(time);

            if (processIndustrialTargetsTimer.Passed)
            {
                processIndustrialTargetsTimer.Reset();
                ProcessIndustrialTargets();
            }
        }

        protected void UpdatePrimaryTarget(TimeSpan time)
        {
            primarySelectTimer.Update(time);

            if (primarySelectTimer.Passed)
            {
                var success = SelectPrimaryTarget();

                SetPrimaryUpdateDelay(success);
            }
        }

        protected void RunModules(TimeSpan time)
        {
            foreach (var activator in moduleActivators)
            {
                activator.Update(time);
            }
        }

        protected virtual TimeSpan SetPrimaryDwellTime()
        {
            return FastRandom.NextTimeSpan(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10));
        }

        protected virtual void SetPrimaryUpdateDelay(bool newPrimary)
        {
            if (newPrimary)
            {
                primarySelectTimer.Interval = SetPrimaryDwellTime();
            }
            else if (GetValidLocks().Length > 0)
            {
                primarySelectTimer.Interval = TimeSpan.FromSeconds(1);
            }
            else if (this.smartCreature.GetLocks().Count > 0)
            {
                primarySelectTimer.Interval = TimeSpan.FromSeconds(1.5);
            }
            else
            {
                primarySelectTimer.Interval = TimeSpan.FromSeconds(3.5);
            }
        }

        protected bool IsAttackable(Hostile hostile)
        {
            if (!hostile.Unit.InZone)
            {
                return false;
            }

            if (hostile.Unit.States.Dead)
            {
                return false;
            }

            if (!hostile.Unit.IsLockable)
            {
                return false;
            }

            if (hostile.Unit.IsAttackable != ErrorCodes.NoError)
            {
                return false;
            }

            if (hostile.Unit.IsInvulnerable)
            {
                return false;
            }

            if (this.smartCreature.Behavior.Type == BehaviorType.Neutral && hostile.IsExpired)
            {
                return false;
            }

            var isVisible = this.smartCreature.IsVisible(hostile.Unit);

            if (!isVisible)
            {
                return false;
            }

            return true;
        }

        protected virtual void ProcessIndustrialTargets()
        {
            if (!this.smartCreature.IndustrialValueManager.IndustrialTargets.Any())
            {
                if (this.smartCreature.GetLocks().Any())
                {
                    this.smartCreature.ResetLocks();
                }

                return;
            }

            var industrialTargetsEnumerator = this.smartCreature.IndustrialValueManager.IndustrialTargets.GetEnumerator();

            while (industrialTargetsEnumerator.MoveNext())
            {
                var industrialTarget = industrialTargetsEnumerator.Current;

                if (!this.smartCreature.IsInLockingRange(industrialTarget.Position))
                {
                    continue;
                }

                SetLockForIndustrialTarget(industrialTarget);
            }
        }

        protected bool TryMakeFreeLockSlotFor(IndustrialTarget industrialTarget)
        {
            if (this.smartCreature.HasFreeLockSlot)
            {
                return true;
            }

            this.smartCreature.IndustrialValueManager.IndustrialTargets
                .Where(x => x.IndustrialValue == 0)
                .ForEach(x => this.smartCreature.GetLockByPosition(x.Position).Cancel());

            var weakestLock = this.smartCreature.IndustrialValueManager.IndustrialTargets
                .SkipWhile(h => h != industrialTarget)
                .Skip(1)
                .Select(h => this.smartCreature.GetLockByPosition(h.Position))
                .LastOrDefault();

            if (weakestLock == null)
            {
                return false;
            }

            weakestLock.Cancel();

            return true;
        }

        protected IndustrialTarget GetMostValuableIndustrialTarget()
        {
            var primaryTarget = this.smartCreature.IndustrialValueManager.IndustrialTargets
                .Where(h => h.Position == (this.smartCreature.GetPrimaryLock() as TerrainLock)?.Location)
                .FirstOrDefault();

            if (primaryTarget != null)
            {
                return primaryTarget;
            }

            return this.smartCreature.IndustrialValueManager.GetMostValuableIndustrialTarget();
        }

        private void SetLockForIndustrialTarget(IndustrialTarget industrialTarget)
        {
            var mostValuable = GetMostValuableIndustrialTarget() == industrialTarget;
            var industrialLock = this.smartCreature.GetLockByPosition(industrialTarget.Position);

            if (industrialLock == null)
            {
                if (TryMakeFreeLockSlotFor(industrialTarget))
                {
                    this.smartCreature.AddLock(industrialTarget.Position, mostValuable);
                }
            }
            else
            {
                if (mostValuable && !industrialLock.Primary)
                {
                    this.smartCreature.SetPrimaryLock(industrialLock.Id);
                }
            }
        }

        private bool IsLockValidTarget(TerrainLock industrialLock)
        {
            if (industrialLock == null || industrialLock.State != LockState.Locked)
            {
                return false;
            }

            return industrialLock.Location.IsInRangeOf2D(this.smartCreature.PositionWithHeight, this.smartCreature.MaxIndustrialRange);
        }

        private TerrainLock[] GetValidLocks()
        {
            return this.smartCreature
                .GetLocks()
                .Select(l => (TerrainLock)l)
                .Where(u => IsLockValidTarget(u))
                .ToArray();
        }

        private bool SelectPrimaryTarget()
        {
            var validLocks = GetValidLocks();

            if (validLocks.Length < 1)
            {
                return false;
            }

            return stratSelector?.TryUseStrategy(smartCreature, validLocks) ?? false;
        }
    }
}
