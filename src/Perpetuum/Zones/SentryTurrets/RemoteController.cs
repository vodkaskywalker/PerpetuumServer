using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones.Blobs.BlobEmitters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.Zones.SentryTurrets
{
    public class RemoteController
    {
        private readonly ItemProperty bandwidthMax;
        private BandwidthHandler bandwidthHandler;

        public RemoteController(TurretLauncherModule module)
        {
            bandwidthMax = new ModuleProperty(
                module,
                AggregateField.bandwidth_max);
            module.AddProperty(bandwidthMax);

            InitBandwidthHandler(module);
        }

        public double BandwidthMax
        {
            get { return bandwidthMax.Value; }
        }

        private void InitBandwidthHandler(TurretLauncherModule module)
        {
            bandwidthHandler = new BandwidthHandler(module);
        }

        public void SyncRemoteChannels()
        {
            this.bandwidthHandler.Update();
        }

        [CanBeNull]
        public RemoteChannel GetRemoteChannel(long channelId)
        {
            return bandwidthHandler.GetRemoteChannel(channelId);
        }

        [CanBeNull]
        public RemoteChannel GetRemoteChannelByUnit(Unit unit)
        {
            return bandwidthHandler.GetRemoteChannelByUnit(unit);
        }

        public bool HasFreeBandwidthOf(SentryTurret turret)
        {
            return bandwidthHandler.HasFreeBandwidthOf(turret);
        }

        public void UseRemoteChannel(SentryTurret turret)
        {
            bandwidthHandler.UseRemoteChannel(turret);
            turret.RemoteChannelDeactivated += bandwidthHandler.OnRemoteChannelDeactivated;
        }

        public void UseRemoteChannel(RemoteChannel newChannel)
        {
            bandwidthHandler.UseRemoteChannel(newChannel);
        }
    }
}
