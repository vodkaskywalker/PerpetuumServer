using Perpetuum.Groups.Corporations;
using Perpetuum.Players;
using Perpetuum.Services.Standing;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Zones.RemoteControl
{
    public class SentryTurret : RemoteControlledTurret
    {
        private readonly IStandingHandler standingHandler;
        private const double StandingLimit = 0.0;

        public SentryTurret(IStandingHandler standingHandler)
        {
            this.standingHandler = standingHandler;
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

        internal override bool IsHostile(Npc npc)
        {
            return true;
        }

        protected override void UpdateUnitVisibility(Unit target)
        {
            if (target is Npc)
            {
                UpdateVisibility(target);
            }
        }

        private bool IsHostilePlayer(long playerEid)
        {
            var standing = standingHandler.GetStanding(Owner, playerEid);

            return StandingLimit >= standing;
        }
    }
}