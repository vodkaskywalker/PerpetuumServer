using Perpetuum.Groups.Corporations;
using Perpetuum.Players;
using Perpetuum.Services.Standing;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem;
using System;

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
            if (Player == player)
            {
                return false;
            }

            return IsHostileCorporation(player.CorporationEid);
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

        private bool IsHostileCorporation(long corporationEid)
        {
            if (DefaultCorporationDataCache.IsCorporationDefault(corporationEid))
            {
                return true;
            }

            var standing = standingHandler.GetStanding(Owner, corporationEid);

            return StandingLimit >= standing;
        }
    }
}
