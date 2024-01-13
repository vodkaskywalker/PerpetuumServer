using Perpetuum.Zones.Locking.Locks;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.TargettingStrategies
{
    public delegate bool IndustrialTargetSelectionStrategy(SmartCreature smartCreature, TerrainLock[] locks);

    public static class IndustrialStrategies
    {
        public static Dictionary<IndustrialPrimaryLockSelectionStrategy, IndustrialTargetSelectionStrategy> All =
            new Dictionary<IndustrialPrimaryLockSelectionStrategy, IndustrialTargetSelectionStrategy>()
        {
            { IndustrialPrimaryLockSelectionStrategy.RichestTile, TargetRichestTile },
        };

        public static IndustrialTargetSelectionStrategy GetStrategy(IndustrialPrimaryLockSelectionStrategy strategyType)
        {
            return All.GetOrDefault(strategyType);
        }

        public static bool TryInvokeStrategy(IndustrialPrimaryLockSelectionStrategy strategyType, SmartCreature smartCreature, TerrainLock[] locks)
        {
            var industrialLockSelectionStrategy = GetStrategy(strategyType);

            if (industrialLockSelectionStrategy == null)
            {
                return false;
            }

            return industrialLockSelectionStrategy(smartCreature, locks);
        }

        private static bool TargetRichestTile(SmartCreature smartCreature, TerrainLock[] locks)
        {
            var industrialTargets = smartCreature.IndustrialValueManager.IndustrialTargets;

            if (!industrialTargets.Any())
            {
                return false;
            }

            var industrialTargetLocks = locks.Where(u => industrialTargets.Any(h => h.Position.ToString() == u.Location.ToString()));
            var mostHostileLock = industrialTargetLocks
                .OrderByDescending(u => industrialTargets.FirstOrDefault(h => h.Position.ToString() == u.Location.ToString())?.IndustrialValue ?? 0)
                .FirstOrDefault();

            return TrySetPrimaryLock(smartCreature, mostHostileLock);
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
