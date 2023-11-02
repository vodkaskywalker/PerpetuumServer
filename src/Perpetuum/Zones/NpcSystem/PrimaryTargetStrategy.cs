﻿using Perpetuum.Zones.Locking.Locks;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem
{
    public delegate bool TargetSelectionStrategy(SmartCreature smartCreature, UnitLock[] locks);

    public static class Strategies
    {
        public static Dictionary<PrimaryLockSelectionStrategy, TargetSelectionStrategy> All =
            new Dictionary<PrimaryLockSelectionStrategy, TargetSelectionStrategy>()
        {
            { PrimaryLockSelectionStrategy.Random, TargetRandom },
            { PrimaryLockSelectionStrategy.Hostile, TargetMostHated },
            { PrimaryLockSelectionStrategy.Closest, TargetClosest },
            { PrimaryLockSelectionStrategy.OptimalRange, TargetWithinOptimal }
        };

        public static TargetSelectionStrategy GetStrategy(PrimaryLockSelectionStrategy strategyType)
        {
            return All.GetOrDefault(strategyType);
        }

        public static bool TryInvokeStrategy(PrimaryLockSelectionStrategy strategyType, SmartCreature smartCreature, UnitLock[] locks)
        {
            var primaryLockSelectionStrategy = GetStrategy(strategyType);

            if (primaryLockSelectionStrategy == null)
            {
                return false;
            }

            return primaryLockSelectionStrategy(smartCreature, locks);
        }

        private static bool TargetMostHated(SmartCreature smartCreature, UnitLock[] locks)
        {
            var hostiles = smartCreature.ThreatManager.Hostiles;
            var hostileLocks = locks.Where(u => hostiles.Any(h => h.unit.Eid == u.Target.Eid));
            var mostHostileLock = hostileLocks
                .OrderByDescending(u => hostiles.Where(h => h.unit.Eid == u.Target.Eid).FirstOrDefault()?.Threat ?? 0)
                .FirstOrDefault();

            return TrySetPrimaryLock(smartCreature, mostHostileLock);
        }

        private static bool TargetWithinOptimal(SmartCreature smartCreature, UnitLock[] locks)
        {
            return TrySetPrimaryLock(
                smartCreature,
                locks
                    .Where(k => k.Target.GetDistance(smartCreature) < smartCreature.BestCombatRange)
                    .RandomElement());
        }

        private static bool TargetClosest(SmartCreature smartCreature, UnitLock[] locks)
        {
            return TrySetPrimaryLock(smartCreature, locks.OrderBy(u => u.Target.GetDistance(smartCreature)).First());
        }

        private static bool TargetRandom(SmartCreature smartCreature, UnitLock[] locks)
        {
            return TrySetPrimaryLock(smartCreature, locks.RandomElement());
        }

        private static bool TrySetPrimaryLock(SmartCreature smartCreature, Lock targetLock)
        {
            if (targetLock == null)
            {
                return false;
            }

            smartCreature.SetPrimaryLock(targetLock);

            return true;
        }
    }
}
