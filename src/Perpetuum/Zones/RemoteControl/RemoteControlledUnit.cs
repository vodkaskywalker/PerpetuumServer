using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;

namespace Perpetuum.Zones.RemoteControl
{
    public class RemoteControlledUnit : Ammo
    {
        private ItemProperty rcBandwidthUsage = ItemProperty.None;

        public override void Initialize()
        {
            rcBandwidthUsage = new AmmoProperty<RemoteControlledUnit>(this, AggregateField.remote_control_bandwidth_usage);
            AddProperty(rcBandwidthUsage);

            base.Initialize();
        }

        public double RemoteChannelBandwidthUsage
        {
            get { return rcBandwidthUsage.Value; }
        }
    }
}
