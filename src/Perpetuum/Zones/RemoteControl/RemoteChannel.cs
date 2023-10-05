using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Locking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perpetuum.Timers;
using Perpetuum.Modules;

namespace Perpetuum.Zones.RemoteControl
{
    public delegate void RemoteChannelEventHandler(SentryTurret turret);
    public delegate void RemoteChannelEventHandler<in T>(RemoteChannel channel, T arg);

    public class RemoteChannel
    {
        public long Id { get; private set; }

        public RemoteChannel(RemoteControllerModule owner, SentryTurret turret)
        {
            Id = FastRandom.NextLong();
            Owner = owner;
            Turret = turret;
        }

        public RemoteControllerModule Owner { get; private set; }

        public SentryTurret Turret { get; set; }

        public virtual bool Equals(Lock other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }
    }
}
