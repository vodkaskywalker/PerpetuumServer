using Perpetuum.Accounting.Characters;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones.Blobs;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Locking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Modules;

namespace Perpetuum.Zones.RemoteControl
{
    public class BandwidthHandler
    {
        private readonly RemoteControllerModule owner;
        private List<RemoteChannel> channels = new List<RemoteChannel>();
        private readonly ConcurrentQueue<RemoteChannel> newChannels = new ConcurrentQueue<RemoteChannel>();
        private readonly ConcurrentQueue<RemoteChannel> deactivatedChannels = new ConcurrentQueue<RemoteChannel>();
        private int dirty;

        public BandwidthHandler(RemoteControllerModule owner)
        {
            this.owner = owner;
        }

        public double BandwidthUsed 
        { 
            get
            {
                return channels.Any()
                    ? channels.Sum(x => x.Turret?.RemoteChannelBandwidthUsage ?? 0)
                    : 0;
            }
        }

        public List<RemoteChannel> Turrets
        {
            get { return channels; }
        }

        public bool HasFreeBandwidthFor(RemoteControlledUnit unit)
        {
            return BandwidthUsed <= owner.BandwidthMax - unit.RemoteChannelBandwidthUsage;
        }

        public void UseRemoteChannel(SentryTurret turret)
        {
            UseRemoteChannel(new RemoteChannel(owner, turret));
        }

        public void UseRemoteChannel(RemoteChannel newChannel)
        {
            newChannels.Enqueue(newChannel);
            Interlocked.Exchange(ref dirty, 1);
        }

        public void OnRemoteChannelDeactivated(SentryTurret turret)
        {
            var channel = GetRemoteChannelByUnit(turret);
            if (channel != null)
            {
                ReleaseRemoteChannel(channel);
                turret.RemoteChannelDeactivated -= OnRemoteChannelDeactivated;
            }
        }

        private void ReleaseRemoteChannel(RemoteChannel channel)
        {
            deactivatedChannels.Enqueue(channel);
            Interlocked.Exchange(ref dirty, 1);
        }

        private void ProcessReleasedChannels(IList<RemoteChannel> channels)
        {
            RemoteChannel releasedChannel;
            while (deactivatedChannels.TryDequeue(out releasedChannel))
            {
                channels.Remove(releasedChannel);
            }
        }

        public void Update()
        {
            if (Interlocked.CompareExchange(ref dirty, 0, 1) == 1)
            {
                var channelsToProcess = this.channels.ToList();
                try
                {
                    ProcessReleasedChannels(channelsToProcess);
                    ProcessNewChannels(channelsToProcess);
                }
                finally
                {
                    this.channels = channelsToProcess;
                }
            }
        }

        private void ProcessNewChannels(IList<RemoteChannel> channels)
        {
            RemoteChannel newChannel;
            while (newChannels.TryDequeue(out newChannel))
            {
                if (channels.Any(l => l.Equals(newChannel)))
                {
                    continue;
                }

                channels.Add(newChannel);
            }
        }

        [CanBeNull]
        public RemoteChannel GetRemoteChannel(long channelId)
        {
            if (channelId == 0)
            {
                return null;
            }

            return channels.Find(l => l.Id == channelId);
        }

        [CanBeNull]
        public RemoteChannel GetRemoteChannelByUnit(Unit unit)
        {
            return GetRemoteChannelByEid(unit.Eid);
        }

        [CanBeNull]
        private RemoteChannel GetRemoteChannelByEid(long unitEid)
        {
            return channels.OfType<RemoteChannel>().FirstOrDefault(l => l.Turret.Eid == unitEid);
        }
    }
}
