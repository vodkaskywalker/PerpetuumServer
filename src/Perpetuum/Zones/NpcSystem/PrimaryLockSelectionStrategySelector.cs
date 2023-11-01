using Perpetuum.Collections;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Zones.NpcSystem
{
    public class PrimaryLockSelectionStrategySelector
    {
        private readonly WeightedCollection<PrimaryLockSelectionStrategy> primaryLockSelectionStrategies;

        public PrimaryLockSelectionStrategySelector(WeightedCollection<PrimaryLockSelectionStrategy> primaryLockSelectionStrategies)
        {
            this.primaryLockSelectionStrategies = primaryLockSelectionStrategies;
        }

        public bool TryUseStrategy(Npc npc, UnitLock[] locks)
        {
            var primaryLockSelectionStrategy = primaryLockSelectionStrategies.GetRandom();

            return Strategies.TryInvokeStrategy(primaryLockSelectionStrategy, npc, locks);
        }

        public bool TryUseStrategy(SmartCreature smartCreature, UnitLock[] locks)
        {
            var primaryLockSelectionStrategy = primaryLockSelectionStrategies.GetRandom();

            return Strategies.TryInvokeStrategy(primaryLockSelectionStrategy, smartCreature, locks);
        }

        public static PrimaryLockSelectionStrategyBuilder Create()
        {
            return new PrimaryLockSelectionStrategyBuilder();
        }
    }
}
