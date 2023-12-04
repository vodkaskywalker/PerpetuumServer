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
            { IndustrialPrimaryLockSelectionStrategy.RichestTileWithinOptimal, TargetRichestTileWithinOptimal },
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

        private static bool TargetRichestTileWithinOptimal(SmartCreature smartCreature, TerrainLock[] locks)
        {
            var industrialTargets = smartCreature.IndustrialValueManager.IndustrialTargets;
            var industrialTargetLocks = locks.Where(u => industrialTargets.Any(h => h.Position.ToString() == u.Location.ToString()));
            var mostHostileLock = industrialTargetLocks
                .Where(k => k.Location.IsInRangeOf3D(smartCreature.PositionWithHeight, smartCreature.BestCombatRange))
                .OrderByDescending(u => industrialTargets.Where(h => h.Position.ToString() == u.Location.ToString()).FirstOrDefault()?.IndustrialValue ?? 0)
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
