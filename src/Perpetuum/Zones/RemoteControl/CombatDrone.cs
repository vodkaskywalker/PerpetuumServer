using Perpetuum.Players;
using Perpetuum.Services.Standing;
using Perpetuum.Units;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem;
using System;
using System.Linq;

namespace Perpetuum.Zones.RemoteControl
{
    public class CombatDrone : RemoteControlledCreature
    {
        private readonly IStandingHandler standingHandler;
        private const double StandingLimit = 0.0;

        public double GuardRange { get; set; }

        public override bool IsStationary => false;

        public CombatDrone(IStandingHandler standingHandler)
        {
            this.standingHandler = standingHandler;
        }

        public bool IsInGuardRange
        {
            get { return CurrentPosition.IsInRangeOf2D(HomePosition, GuardRange); }
        }

        protected override bool IsHostileFor(Unit unit)
        {
            return unit.IsHostile(this);
        }

        public override bool IsHostile(Player player)
        {
            if (Player != null && Player == player)
            {
                return false;
            }

            if (Player.Gang != null && Player.Gang.IsMember(player.Character))
            {
                return false;
            }

            return IsHostilePlayer(player.Eid);
        }

        public override void OnAggression(Unit victim)
        {
            this.Player.OnAggression(victim);
        }

        /*
        public override void LookingForHostiles()
        {
            foreach (var visibility in GetVisibleUnits().Where(x=>IsCommandBotPrimaryLock(x.Target))
            {
                AddBodyPullThreat(visibility.Target);
            }
        }
        */

        protected override void OnUpdate(TimeSpan time)
        {
            HomePosition = Player.CurrentPosition;
            base.OnUpdate(time);
        }

        internal override bool IsHostile(Npc npc)
        {
            return true;
        }

        protected override void OnUnitLockStateChanged(Lock @lock)
        {
            // Do nothing
        }

        protected override void UpdateUnitVisibility(Unit target)
        {
            if (target is Npc)
            {
                UpdateVisibility(target);
            }
        }

        protected override bool IsDetected(Unit target)
        {
            if (!IsCommandBotPrimaryLock(target))
            {
                return false;
            }

            return base.IsDetected(target);
        }

        private bool IsHostilePlayer(long playerEid)
        {
            var standing = standingHandler.GetStanding(Owner, playerEid);

            return StandingLimit >= standing;
        }

        private bool IsCommandBotPrimaryLock(Unit unit)
        {
            var primaryLock = this.Player.GetPrimaryLock();

            if (primaryLock != null &&
                primaryLock.State == Locking.LockState.Locked &&
                primaryLock is UnitLock &&
                (primaryLock as UnitLock).Target == unit)
            {
                return true;
            }

            return false;
        }
    }
}
