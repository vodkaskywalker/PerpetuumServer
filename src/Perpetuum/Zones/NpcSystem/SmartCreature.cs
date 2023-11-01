using Perpetuum.EntityFramework;
using Perpetuum.StateMachines;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem.AI;
using Perpetuum.Zones.NpcSystem.Flocks;
using System;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem
{
    #region move to separate files



    #endregion

    public class SmartCreature : Creature
    {
        private const double AggroRange = 30;
        private const double BestComnatRangeModifier = 0.9;
        private readonly ThreatManager threatManager;
        private readonly IPseudoThreatManager pseudoThreatManager;
        private readonly Lazy<int> maxCombatRange;
        private readonly Lazy<int> optimalCombatRange;
        private readonly TimeKeeper debounceBodyPull = new TimeKeeper(TimeSpan.FromSeconds(2.5));
        private readonly TimeKeeper debounceLockChange = new TimeKeeper(TimeSpan.FromSeconds(2.5));
        private readonly IntervalTimer pseudoUpdateFreq = new IntervalTimer(TimeSpan.FromMilliseconds(650));

        [CanBeNull]
        private INpcGroup group;

        public StackFSM AI { get; private set; }

        public SmartCreatureBehavior Behavior { get; set; }

        public double HomeRange { get; set; }

        public Position HomePosition { get; set; }

        public IThreatManager ThreatManager
        {
            get { return this.threatManager; }
        }

        public int BestCombatRange
        {
            get { return this.optimalCombatRange.Value; }
        }

        public int MaxCombatRange
        {
            get { return this.maxCombatRange.Value; }
        }

        public bool IsInHomeRange
        {
            get { return CurrentPosition.IsInRangeOf2D(HomePosition, HomeRange); }
        }

        public INpcGroup Group
        {
            get { return group; }
        }

        public virtual bool IsStationary
        {
            get { return MaxSpeed.IsZero(); }
        }

        public SmartCreature()
        {
            maxCombatRange = new Lazy<int>(CalculateMaxCombatRange);
            optimalCombatRange = new Lazy<int>(CalculateCombatRange);
            threatManager = new ThreatManager();
            AI = new StackFSM();
            pseudoThreatManager = new PseudoThreatManager();
        }

        public void LookingForHostiles()
        {
            foreach (var visibility in GetVisibleUnits())
            {
                AddBodyPullThreat(visibility.Target);
            }
        }

        public void AddPseudoThreat(Unit hostile)
        {
            pseudoThreatManager.AddOrRefreshExisting(hostile);
        }

        public void SetGroup(INpcGroup group)
        {
            this.group = group;
        }

        protected override void OnUnitLockStateChanged(Lock @lock)
        {
            if (!debounceLockChange.Expired)
            {
                return;
            }

            var unitLock = @lock as UnitLock;

            if (unitLock == null)
            {
                return;
            }

            if (unitLock.Target != this)
            {
                return;
            }

            if (unitLock.State != LockState.Locked)
            {
                return;
            }

            var threatValue = unitLock.Primary
                ? Threat.LOCK_PRIMARY
                : Threat.LOCK_SECONDARY;

            AddThreat(unitLock.Owner, new Threat(ThreatType.Lock, threatValue), true);
            debounceLockChange.Reset();
        }

        protected override void OnUnitTileChanged(Unit target)
        {
            if (!debounceBodyPull.Expired)
            {
                return;
            }

            AddBodyPullThreat(target);
            debounceBodyPull.Reset();
        }

        private void AddBodyPullThreat(Unit enemy)
        {
            if (!IsHostile(enemy))
            {
                return;
            }

            var helper = new SmartCreatureBodyPullThreatHelper(this);

            enemy.AcceptVisitor(helper);
        }

        private int CalculateCombatRange()
        {
            double range = (int)ActiveModules
                .Where(m => m.IsRanged)
                .Select(module => module.OptimalRange)
                .Concat(new[] { MaxTargetingRange })
                .Min();

            range *= BestComnatRangeModifier;
            range = Math.Max(3, range);

            return (int)range;
        }

        private int CalculateMaxCombatRange()
        {
            double range = ActiveModules.Where(m => m.IsRanged)
                         .Select(module => (int)(module.OptimalRange + module.Falloff))
                         .Max();

            range = Math.Max(3, range);

            return (int)range;
        }

        public bool IsInAggroRange(Unit target)
        {
            return IsStationary || IsInRangeOf3D(target, AggroRange);
        }

        public void AddThreat(Unit hostile, Threat threat, bool spreadToGroup)
        {
            //if (hostile.IsPlayer())
            //{
            //    BossInfo?.OnAggro(hostile as Player);
            //}

            threatManager.GetOrAddHostile(hostile).AddThreat(threat);

            RemovePseudoThreat(hostile);

            if (!spreadToGroup)
            {
                return;
            }

            var group = Group;
            if (@group == null)
            {
                return;
            }

            //var multipliedThreat = Threat.Multiply(threat, 0.5);

            //foreach (var member in @group.Members)
            //{
            //    if (member == this)
            //    {
            //        continue;
            //    }

            //    member.AddThreat(hostile, multipliedThreat, false);
            //}
        }

        /// <summary>
        /// This determines if threat can be added to a target based on the following:
        ///  - Is the target already on the threat manager
        ///  - Or is the npc non-passive and the Threat is of some defined type
        /// </summary>
        /// <param name="target">Unit target</param>
        /// <param name="threat">Threat threat</param>
        /// <returns>If the target can be a threat</returns>
        public bool CanAddThreatTo(Unit target, Threat threat)
        {
            if (threatManager.Contains(target))
            {
                return true;
            }

            if (Behavior.Type == SmartCreatureBehaviorType.Passive)
            {
                return false;
            }

            return threat.type != ThreatType.Undefined;
        }

        public void AddAssistThreat(Unit assistant, Unit target, Threat threat)
        {
            if (!threatManager.Contains(target))
            {
                return;
            }

            if (!CanAddThreatTo(assistant, threat))
                return;

            AddThreat(assistant, threat, true);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void OnTileChanged()
        {
            base.OnTileChanged();
            LookingForHostiles();
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            States.Aggressive = Behavior.Type == SmartCreatureBehaviorType.Aggressive;

            base.OnEnterZone(zone, enterType);

            if (IsStationary)
            {
                AI.Push(new SmartCreatureStationaryIdleAI(this));
            }
            else
            {
                AI.Push(new SmartCreatureIdleAI(this));
            }
        }

        protected override void OnDamageTaken(Unit source, DamageTakenEventArgs e)
        {
            base.OnDamageTaken(source, e);

            var player = Zone.ToPlayerOrGetOwnerPlayer(source);
            if (player == null)
            {
                return;
            }

            //BossInfo?.OnDamageTaken(this, player);
            AddThreat(player, new Threat(ThreatType.Damage, e.TotalDamage * 0.9), true);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);

            AI.Update(time);

            UpdatePseudoThreats(time);
        }

        private void RemovePseudoThreat(Unit hostile)
        {
            pseudoThreatManager.Remove(hostile);
        }

        private void UpdatePseudoThreats(TimeSpan time)
        {
            pseudoUpdateFreq.Update(time);

            if (pseudoUpdateFreq.Passed)
            {
                pseudoThreatManager.Update(pseudoUpdateFreq.Elapsed);
                pseudoUpdateFreq.Reset();
            }
        }
    }
}
