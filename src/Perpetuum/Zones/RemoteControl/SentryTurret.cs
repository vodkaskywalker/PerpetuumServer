using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.LandMines;
using Perpetuum.Zones.NpcSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.Zones.RemoteControl
{
    public class SentryTurret : Turret
    {
        private UnitDespawnHelper _despawnHelper;

        public event RemoteChannelEventHandler RemoteChannelDeactivated;

        private ItemProperty rcBandwidthUsage = ItemProperty.None;

        public double RemoteChannelBandwidthUsage
        {
            get { return rcBandwidthUsage.Value; }
        }

        public TimeSpan DespawnTime
        {
            set { _despawnHelper = UnitDespawnHelper.Create(this, value); }
        }

        public override void Initialize()
        {
            rcBandwidthUsage = new UnitProperty(this, AggregateField.remote_control_bandwidth_usage);
            AddProperty(rcBandwidthUsage);

            base.Initialize();
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            _despawnHelper?.Update(time, this);
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
    }
}
