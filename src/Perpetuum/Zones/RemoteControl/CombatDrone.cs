using Perpetuum.EntityFramework;
using Perpetuum.Players;
using Perpetuum.Services.Standing;
using Perpetuum.Units;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem;
using System;

namespace Perpetuum.Zones.RemoteControl
{
    public class CombatDrone : RemoteControlledCreature
    {
        public double GuardRange { get; set; }

        public override bool IsStationary => false;

        public CombatDrone(IStandingHandler standingHandler)
            : base(standingHandler)
        {
        }

        public bool IsInGuardRange => CurrentPosition.IsInRangeOf2D(HomePosition, GuardRange);

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override bool IsHostile(Player player)
        {
            return IsHostilePlayer(player);
        }

        public override void OnAggression(Unit victim)
        {
            CommandRobot.OnAggression(victim);
        }

        protected override bool IsHostileFor(Unit unit)
        {
            return unit.IsHostile(this);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            HomePosition = CommandRobot.CurrentPosition;
            base.OnUpdate(time);
        }

        protected override bool IsDetected(Unit target)
        {
            return IsCommandBotPrimaryLock(target) && base.IsDetected(target);
        }

        internal override bool IsHostile(Npc npc)
        {
            return true;
        }

        internal override bool IsHostile(CombatDrone drone)
        {
            return !(drone.CommandRobot is Player player) || IsHostilePlayer(player);
        }

        internal override bool IsHostile(SentryTurret turret)
        {
            return !(turret.CommandRobot is Player player) || IsHostilePlayer(player);
        }

        protected override void OnUnitLockStateChanged(Lock @lock)
        {
            // Do nothing
        }

        protected override void UpdateUnitVisibility(Unit target)
        {
            if (target is Npc ||
                target is RemoteControlledCreature)
            {
                UpdateVisibility(target);
            }
        }

        private bool IsCommandBotPrimaryLock(Unit unit)
        {
            Lock primaryLock = CommandRobot.GetPrimaryLock();

            return primaryLock != null &&
                primaryLock.State == Locking.LockState.Locked &&
                primaryLock is UnitLock &&
                (primaryLock as UnitLock).Target == unit;
        }
    }
}
