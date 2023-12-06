using System.Collections.Immutable;

namespace Perpetuum.Zones.NpcSystem.IndustrialTargetsManagement
{
    public interface IIndustrialValueManager
    {
        bool IsValuable { get; }

        ImmutableSortedSet<IndustrialTarget> IndustrialTargets { get; }

        bool Contains(Position tile);

        void Remove(IndustrialTarget industrialTarget);

        void Clear();
    }
}
