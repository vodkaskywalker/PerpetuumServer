using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Players;
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
        private ItemProperty remoteChannelBandwidthUsage = ItemProperty.None;

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

        protected override void OnRemovedFromZone(IZone zone)
        {
            RemoteChannelDeactivated(this);
            base.OnRemovedFromZone(zone);
        }

        protected override void OnDead(Unit killer)
        {
            base.OnDead(killer);
        }
    }
}
