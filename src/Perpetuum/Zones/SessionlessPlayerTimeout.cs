﻿using Perpetuum.Comparers;
using Perpetuum.EntityFramework;
using Perpetuum.Players;
using Perpetuum.Timers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Perpetuum.Zones
{
    /// <summary>
    /// A class used by a Zone to determine if a Player is orphaned by a timeout check on its ZoneSession
    /// </summary>
    public class SessionlessPlayerTimeout
    {
        private static readonly TimeSpan MAX_ORPHAN_TIME = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan UPDATE_RATE = TimeSpan.FromMinutes(1.5);
        private readonly IZone _zone;
        private readonly List<PlayerTimeout> _orphanPlayers = new List<PlayerTimeout>();

        public SessionlessPlayerTimeout(IZone zone)
        {
            _zone = zone;
        }

        private TimeSpan _elapsed = TimeSpan.Zero;
        public void Update(TimeSpan time)
        {
            _elapsed += time;
            if (_elapsed < UPDATE_RATE)
                return;

            DoUpdate();
            _elapsed = TimeSpan.Zero;
        }

        private void DoUpdate()
        {
            PopulateNewSessionlessPlayers(_zone);
            RemoveExpiredOrphansFromZone(_orphanPlayers);
        }

        private void PopulateNewSessionlessPlayers(IZone zone)
        {
            var newOrphans =
                zone.Players
                    .Where(p => p.Session == ZoneSession.None)
                    .Cast<IEntity>()
                    .Except(_orphanPlayers, new EntityComparer())
                    .Cast<Player>()
                    .Select(p => new PlayerTimeout(p));

            _orphanPlayers.AddRange(newOrphans);
        }

        private void RemoveExpiredOrphansFromZone(IEnumerable<PlayerTimeout> orphans)
        {
            var toRemove = orphans.Where(o => o.Expired);

            // The Enumerable ForEach call was deprecated for good reason
            // use a standard foreach loop so that you have compiler help
            foreach (var orphan in toRemove)
                orphan.RemoveFromZone();

            _orphanPlayers.RemoveAll(p => p.Expired);
        }

        private class PlayerTimeout : IEntity
        {
            private readonly TimeKeeper _time;
            private Player Player { get; }

            public bool Expired { get { return _time.Expired; } }
            public long Eid { get { return Player is null ? 0 : Player.Eid; } }

            public PlayerTimeout(Player p)
            {
                _time = new TimeKeeper(MAX_ORPHAN_TIME);
                Player = p;
            }

            public void RemoveFromZone()
            {
                Player.RemoveFromZone();
            }

            public bool Equals(PlayerTimeout other)
            {
                if (other is null)
                    return false;

                return ReferenceEquals(this, other) || Eid == other.Eid;
            }

            public override bool Equals(object obj)
            {
                if (obj is null)
                {
                    return false;
                }
                else if (obj is PlayerTimeout pt)
                {
                    return Equals(pt);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Eid.GetHashCode();
            }
        }
    }
}