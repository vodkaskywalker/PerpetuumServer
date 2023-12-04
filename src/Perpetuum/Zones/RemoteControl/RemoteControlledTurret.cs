using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using System;

namespace Perpetuum.Zones.RemoteControl
{
    public class RemoteControlledTurret : SmartCreature
    {
        private const double SentryTurretCallForHelpArmorThreshold = 0.8;
        private UnitDespawnHelper despawnHelper;
        private ItemProperty rcBandwidthUsage = ItemProperty.None;

        public event RemoteChannelEventHandler RemoteChannelDeactivated;

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

        public override double CallForHelpArmorThreshold => SentryTurretCallForHelpArmorThreshold;

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            despawnHelper?.Update(time, this);
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
