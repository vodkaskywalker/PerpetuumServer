using Perpetuum.Collections;

namespace Perpetuum.Zones.NpcSystem
{
    public class PrimaryLockSelectionStrategyBuilder
    {
        private readonly WeightedCollection<PrimaryLockSelectionStrategy> selection;

        public PrimaryLockSelectionStrategyBuilder()
        {
            selection = new WeightedCollection<PrimaryLockSelectionStrategy>();
        }

        public PrimaryLockSelectionStrategyBuilder WithStrategy(PrimaryLockSelectionStrategy strategy, int weight = 1)
        {
            selection.Add(strategy, weight);

            return this;
        }

        public PrimaryLockSelectionStrategySelector Build()
        {
            return new PrimaryLockSelectionStrategySelector(selection);
        }
    }
}
