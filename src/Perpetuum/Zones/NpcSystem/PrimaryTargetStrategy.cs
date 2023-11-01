using Perpetuum.Zones.Locking.Locks;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem
{
    public delegate bool TargetSelectionStrategy(Npc npc, UnitLock[] locks);
    public delegate bool SmartCreatureTargetSelectionStrategy(SmartCreature smartCreature, UnitLock[] locks);

    public static class Strategies
    {
        public static Dictionary<PrimaryLockSelectionStrategy, TargetSelectionStrategy> All = new Dictionary<PrimaryLockSelectionStrategy, TargetSelectionStrategy>()
        {
            { PrimaryLockSelectionStrategy.Random, TargetRandom },
            { PrimaryLockSelectionStrategy.Hostile, TargetMostHated },
            { PrimaryLockSelectionStrategy.Closest, TargetClosest },
            { PrimaryLockSelectionStrategy.OptimalRange, TargetWithinOptimal }
        };

        public static Dictionary<PrimaryLockSelectionStrategy, SmartCreatureTargetSelectionStrategy> SmartCreatureAll =
            new Dictionary<PrimaryLockSelectionStrategy, SmartCreatureTargetSelectionStrategy>()
        {
            { PrimaryLockSelectionStrategy.Random, SmartCreatureTargetRandom },
            { PrimaryLockSelectionStrategy.Hostile, SmartCreatureTargetMostHated },
            { PrimaryLockSelectionStrategy.Closest, SmartCreatureTargetClosest },
            { PrimaryLockSelectionStrategy.OptimalRange, SmartCreatureTargetWithinOptimal }
        };

        public static TargetSelectionStrategy GetStrategy(PrimaryLockSelectionStrategy strategyType)
        {
            return All.GetOrDefault(strategyType);
        }

        public static SmartCreatureTargetSelectionStrategy SmartCreatureGetStrategy(PrimaryLockSelectionStrategy strategyType)
        {
            return SmartCreatureAll.GetOrDefault(strategyType);
        }

        public static bool TryInvokeStrategy(PrimaryLockSelectionStrategy strategyType, Npc npc, UnitLock[] locks)
        {
            var strat = GetStrategy(strategyType);
            if (strat == null)
                return false;
            return strat(npc, locks);
        }

        public static bool TryInvokeStrategy(PrimaryLockSelectionStrategy strategyType, SmartCreature smartCreature, UnitLock[] locks)
        {
            var primaryLockSelectionStrategy = SmartCreatureGetStrategy(strategyType);

            if (primaryLockSelectionStrategy == null)
            {
                return false;
            }

            return primaryLockSelectionStrategy(smartCreature, locks);
        }

        private static bool TargetMostHated(Npc npc, UnitLock[] locks)
        {
            var hostiles = npc.ThreatManager.Hostiles;
            var hostileLocks = locks.Where(u => hostiles.Any(h => h.unit.Eid == u.Target.Eid));
            var mostHostileLock = hostileLocks.OrderByDescending(u => hostiles.Where(h => h.unit.Eid == u.Target.Eid).FirstOrDefault()?.Threat ?? 0).FirstOrDefault();
            return TrySetPrimaryLock(npc, mostHostileLock);
        }

        private static bool SmartCreatureTargetMostHated(SmartCreature smartCreature, UnitLock[] locks)
        {
            var hostiles = smartCreature.ThreatManager.Hostiles;
            var hostileLocks = locks.Where(u => hostiles.Any(h => h.unit.Eid == u.Target.Eid));
            var mostHostileLock = hostileLocks
                .OrderByDescending(u => hostiles.Where(h => h.unit.Eid == u.Target.Eid).FirstOrDefault()?.Threat ?? 0)
                .FirstOrDefault();

            return SmartCreatureTrySetPrimaryLock(smartCreature, mostHostileLock);
        }

        private static bool TargetWithinOptimal(Npc npc, UnitLock[] locks)
        {
            return TrySetPrimaryLock(npc, locks.Where(k => k.Target.GetDistance(npc) < npc.BestCombatRange).RandomElement());
        }

        private static bool SmartCreatureTargetWithinOptimal(SmartCreature smartCreature, UnitLock[] locks)
        {
            return SmartCreatureTrySetPrimaryLock(smartCreature, locks.Where(k => k.Target.GetDistance(smartCreature) < smartCreature.BestCombatRange).RandomElement());
        }

        private static bool TargetClosest(Npc npc, UnitLock[] locks)
        {
            return TrySetPrimaryLock(npc, locks.OrderBy(u => u.Target.GetDistance(npc)).First());
        }

        private static bool SmartCreatureTargetClosest(SmartCreature smartCreature, UnitLock[] locks)
        {
            return SmartCreatureTrySetPrimaryLock(smartCreature, locks.OrderBy(u => u.Target.GetDistance(smartCreature)).First());
        }

        private static bool TargetRandom(Npc npc, UnitLock[] locks)
        {
            return TrySetPrimaryLock(npc, locks.RandomElement());
        }

        private static bool SmartCreatureTargetRandom(SmartCreature smartCreature, UnitLock[] locks)
        {
            return SmartCreatureTrySetPrimaryLock(smartCreature, locks.RandomElement());
        }

        private static bool TrySetPrimaryLock(Npc npc, Lock l)
        {
            if (l == null) return false;
            npc.SetPrimaryLock(l);
            return true;
        }

        private static bool SmartCreatureTrySetPrimaryLock(SmartCreature smartCreature, Lock targetLock)
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
