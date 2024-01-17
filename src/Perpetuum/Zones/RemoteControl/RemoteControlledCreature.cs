using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Services.Standing;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem;
using System;

namespace Perpetuum.Zones.RemoteControl
{
    public class RemoteControlledCreature : SmartCreature
    {
        private const double SentryTurretCallForHelpArmorThreshold = 0.8;
        private UnitDespawnHelper despawnHelper;
        private ItemProperty remoteChannelBandwidthUsage = ItemProperty.None;
        private readonly IStandingHandler standingHandler;
        private const double StandingLimit = 0.0;

        public event RemoteChannelEventHandler RemoteChannelDeactivated;

        public Player Player { get; private set; }

        public double RemoteChannelBandwidthUsage { get; private set; }

        public TimeSpan DespawnTime
        {
            set
            {
                despawnHelper = UnitDespawnHelper.Create(this, value);
            }
        }

        public override bool IsStationary => true;

        public override double CallForHelpArmorThreshold => SentryTurretCallForHelpArmorThreshold;

        public RemoteControlledCreature(IStandingHandler standingHandler)
        {
            this.standingHandler = standingHandler;
        }

        public void SetPlayer(Player player)
        {
            Player = player;
        }

        public void SetBandwidthUsage(double value)
        {
            RemoteChannelBandwidthUsage = value;
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            despawnHelper?.Update(time, this);
        }

        protected override void OnBeforeRemovedFromZone(IZone zone)
        {
            RemoteChannelDeactivated(this);
        }

        protected override void OnDead(Unit killer)
        {
            base.OnDead(killer);
        }

        protected bool IsHostilePlayer(Player targetPlayer)
        {
            if (Zone.Configuration.IsAlpha && !Player.HasPvpEffect && !targetPlayer.HasPvpEffect)
            {
                return false;
            }

            if (Player != null && Player == targetPlayer)
            {
                return false;
            }

            if (Player.Gang != null && Player.Gang.IsMember(targetPlayer.Character))
            {
                return false;
            }

            var corporationStanding = standingHandler.GetStanding(Player.CorporationEid, targetPlayer.CorporationEid);

            if (corporationStanding > StandingLimit)
            {
                return false;
            }

            var personalStanding = standingHandler.GetStanding(Owner, targetPlayer.Character.Eid);

            if (personalStanding > StandingLimit)
            {
                return false;
            }

            return true;
        }
    }
}
