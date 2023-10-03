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

namespace Perpetuum.Zones.SentryTurrets
{
    public class BandwidthHandler
    {
        private readonly Robot owner;
        //private readonly ItemProperty bandwidth;
        private readonly int bandwidth;
        private List<RemoteChannel> channels = new List<RemoteChannel>();
        private readonly ConcurrentQueue<RemoteChannel> newChannels = new ConcurrentQueue<RemoteChannel>();
        private readonly ConcurrentQueue<RemoteChannel> deactivatedChannels = new ConcurrentQueue<RemoteChannel>();
        private int dirty;

        public BandwidthHandler(Robot owner)
        {
            this.owner = owner;

            bandwidth = 15;

            //bandwidth = new BandwidthMaxProperty(owner);
            //owner.AddProperty(bandwidth);
        }

        //private int Bandwidth { get { return (int)bandwidth.Value; } }

        private int Bandwidth { get { return bandwidth; } }

        public int BandwidthUsed 
        { 
            get
            {
                return channels.Any()
                    ? channels.Sum(x => x.Turret?.BandwidthUsage ?? 0)
                    : 0;
            }
        }

        public List<RemoteChannel> Turrets
        {
            get { return channels; }
        }

        public bool HasFreeBandwidthOf(SentryTurret turret)
        {
            return BandwidthUsed <= Bandwidth - turret.BandwidthUsage;
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

        //private class BandwidthMaxProperty : UnitProperty
        //{
        //    public BandwidthMaxProperty(Unit owner) : base(owner, AggregateField.bandwidth_max, AggregateField.bandwidth_max_modifier)
        //    {
        //    }
        //}
    }
}
