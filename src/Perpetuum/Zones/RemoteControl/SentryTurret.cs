using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Services.Standing;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.AI;
using System;

namespace Perpetuum.Zones.RemoteControl
{
    public class SentryTurret : SmartCreature
    {
        private UnitDespawnHelper despawnHelper;
        private const double SentryTurretCallForHelpArmorThreshold = 0.8;
        private readonly IStandingHandler standingHandler;
        private const double StandingLimit = 0.0;

        public event RemoteChannelEventHandler RemoteChannelDeactivated;

        private ItemProperty rcBandwidthUsage = ItemProperty.None;

        public double RemoteChannelBandwidthUsage
        {
            get { return rcBandwidthUsage.Value; }
        }

        public TimeSpan DespawnTime
        {
            set { despawnHelper = UnitDespawnHelper.Create(this, value); }
        }

        public SentryTurret(IStandingHandler standingHandler)
        {
            this.standingHandler = standingHandler;
        }

        public override void Initialize()
        {
            rcBandwidthUsage = new UnitProperty(this, AggregateField.remote_control_bandwidth_usage);
            AddProperty(rcBandwidthUsage);
            Behavior = Behavior.Create(BehaviorType.RemoteControlled);

            base.Initialize();
        }

        public override bool IsStationary => true;

        public override double CallForHelpArmorThreshold => SentryTurretCallForHelpArmorThreshold;

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            despawnHelper?.Update(time, this);
        }

        protected override bool IsHostileFor(Unit unit)
        {
            return unit.IsHostile(this);
        }

        internal override bool IsHostile(Player player)
        {
            if (Owner == player.Eid)
            {
                return false;
            }

            return IsHostileCorporation(player.CorporationEid);
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

        protected override void OnRemovedFromZone(IZone zone)
        {
            RemoteChannelDeactivated(this);
            base.OnRemovedFromZone(zone);
        }

        protected override void OnDead(Unit killer)
        {
            Zone.CreateBeam(
                BeamType.arbalest_wreck,
                builder => builder
                    .WithPosition(CurrentPosition)
                    .WithState(BeamState.Hit));

            base.OnDead(killer);
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
