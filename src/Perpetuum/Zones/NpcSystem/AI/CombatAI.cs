﻿using Perpetuum.Modules.Weapons;
using Perpetuum.Timers;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.NpcSystem.TargettingStrategies;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using Perpetuum.Zones.Terrains;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class CombatAI : BaseAI
    {
        private const int UpdateFrequency = 1650;
        private readonly IntervalTimer processHostilesTimer = new IntervalTimer(UpdateFrequency);
        private readonly IntervalTimer primarySelectTimer = new IntervalTimer(UpdateFrequency);
        private List<ModuleActivator> moduleActivators;
        private TimeSpan hostilesUpdateFrequency = TimeSpan.FromMilliseconds(UpdateFrequency);
        private CombatPrimaryLockSelectionStrategySelector stratSelector;

        public bool IsNpcHasMissiles { get; set; } = false;

        public CombatAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Enter()
        {
            stratSelector = InitSelector();
            moduleActivators = this.smartCreature.ActiveModules
                .Select(m => new ModuleActivator(m))
                .ToList();
            IsNpcHasMissiles = this.smartCreature.ActiveModules
                .OfType<MissileWeaponModule>()
                .Any();
            processHostilesTimer.Update(hostilesUpdateFrequency);
            primarySelectTimer.Update(hostilesUpdateFrequency);

            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            UpdateHostiles(time);
            UpdatePrimaryTarget(time);
            RunModules(time);
        }

        protected virtual CombatPrimaryLockSelectionStrategySelector InitSelector()
        {
            return CombatPrimaryLockSelectionStrategySelector.Create()
                .WithStrategy(CombatPrimaryLockSelectionStrategy.Hostile, 9)
                .WithStrategy(CombatPrimaryLockSelectionStrategy.Random, 1)
                .Build();
        }

        protected void UpdateHostiles(TimeSpan time)
        {
            processHostilesTimer.Update(time);

            if (processHostilesTimer.Passed)
            {
                processHostilesTimer.Reset();
                ProcessHostiles();
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

        protected virtual void ProcessHostiles()
        {
            var hostileEnumerator = this.smartCreature.ThreatManager.Hostiles.GetEnumerator();

            while (hostileEnumerator.MoveNext())
            {
                var hostile = hostileEnumerator.Current;

                if (!IsAttackable(hostile))
                {
                    this.smartCreature.ThreatManager.Remove(hostile);
                    this.smartCreature.AddPseudoThreat(hostile.Unit);

                    continue;
                }

                if (!this.smartCreature.IsInLockingRange(hostile.Unit))
                {
                    continue;
                }

                SetLockForHostile(hostile);
            }
        }

        protected bool TryMakeFreeLockSlotFor(Hostile hostile)
        {
            if (this.smartCreature.HasFreeLockSlot)
            {
                return true;
            }

            this.smartCreature.ThreatManager.Hostiles
                .Where(x => x.Threat == 0)
                .ForEach(x => this.smartCreature.GetLockByUnit(x.Unit).Cancel());

            var weakestLock = this.smartCreature.ThreatManager.Hostiles
                .SkipWhile(x => x != hostile)
                .Skip(1)
                .Select(x => this.smartCreature.GetLockByUnit(x.Unit))
                .LastOrDefault();

            if (weakestLock == null)
            {
                return false;
            }

            weakestLock.Cancel();

            return true;
        }

        protected Hostile GetPrimaryOrMostHatedHostile()
        {
            var primaryHostile = this.smartCreature.ThreatManager.Hostiles
                .Where(h => h.Unit == (this.smartCreature.GetPrimaryLock() as UnitLock)?.Target)
                .FirstOrDefault();

            if (primaryHostile != null)
            {
                return primaryHostile;
            }

            return this.smartCreature.ThreatManager.GetMostHatedHostile();
        }

        private void SetLockForHostile(Hostile hostile)
        {
            var mostHated = GetPrimaryOrMostHatedHostile() == hostile;
            var combatLock = this.smartCreature.GetLockByUnit(hostile.Unit);

            if (combatLock == null)
            {
                if (TryMakeFreeLockSlotFor(hostile))
                {
                    this.smartCreature.AddLock(hostile.Unit, mostHated);
                }
            }
            else
            {
                if (mostHated && !combatLock.Primary)
                {
                    this.smartCreature.SetPrimaryLock(combatLock.Id);
                }
            }
        }

        private bool IsLockValidTarget(UnitLock unitLock)
        {
            if (unitLock == null || unitLock.State != LockState.Locked)
            {
                return false;
            }

            var visibility = this.smartCreature.GetVisibility(unitLock.Target);

            if (visibility == null)
            {
                return false;
            }

            var r = visibility.GetLineOfSight(IsNpcHasMissiles);

            if (r != null && r.hit && (r.blockingFlags & BlockingFlags.Plant) == 0)
            {
                return false;
            }

            return unitLock.Target.GetDistance(this.smartCreature) < this.smartCreature.MaxCombatRange;
        }

        private UnitLock[] GetValidLocks()
        {
            return this.smartCreature
                .GetLocks()
                .Select(l => (UnitLock)l)
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