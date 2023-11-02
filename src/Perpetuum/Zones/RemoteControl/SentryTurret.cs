using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Players;
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

        public override void Initialize()
        {
            rcBandwidthUsage = new UnitProperty(this, AggregateField.remote_control_bandwidth_usage);
            AddProperty(rcBandwidthUsage);
            Behavior = Behavior.Create(BehaviorType.RemoteControlled);

            base.Initialize();
        }

        public override bool IsStationary => true;

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
            return false;
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
    }
}
