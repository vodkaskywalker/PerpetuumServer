using Perpetuum.Units;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perpetuum.Zones.SentryTurrets;

namespace Perpetuum.Robots
{
    partial class Robot
    {
        private BandwidthHandler bandwidthHandler;

        private void InitBandwidthHandler()
        {
            bandwidthHandler = new BandwidthHandler(this);
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
