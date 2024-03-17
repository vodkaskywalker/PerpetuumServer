using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Perpetuum.Zones.NpcSystem.IndustrialTargetsManagement
{
    public class IndustrialValueManager : IIndustrialValueManager
    {
        private ImmutableDictionary<string, IndustrialTarget> _industrialTargets = ImmutableDictionary<string, IndustrialTarget>.Empty;

        public ImmutableSortedSet<IndustrialTarget> IndustrialTargets
        {
            get { return _industrialTargets.Values.ToImmutableSortedSet(); }
        }

        public bool IsValuable
        {
            get { return !_industrialTargets.IsEmpty; }
        }

        public IndustrialTarget GetOrAddIndustrialTargetWithValue(Position tile, IndustrialValueType valueType, double value)
        {
            return ImmutableInterlocked.GetOrAdd(ref _industrialTargets, tile.ToString(), eid =>
            {
                var industrialTarget = new IndustrialTarget(tile);

                industrialTarget.AddIndustrialValue(new IndustrialValue(valueType, value));

                return industrialTarget;
            });
        }

        public bool Contains(Position tile)
        {
            return _industrialTargets.ContainsKey(tile.ToString());
        }

        public void Clear()
        {
            _industrialTargets = _industrialTargets.Clear();
        }

        public void Remove(IndustrialTarget industrialTarget)
        {
            ImmutableInterlocked.TryRemove(ref _industrialTargets, industrialTarget.Position.ToString(), out _);
        }

        public string ToDebugString()
        {
            if (_industrialTargets.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine("========== THREAT ==========");
            sb.AppendLine();

            foreach (var industrialTarget in _industrialTargets.Values.OrderByDescending(h => h.IndustrialValue))
            {
                sb.AppendFormat("  {0} ({1}) => {2}", "Industrial Target", industrialTarget.Position.ToString(), industrialTarget.IndustrialValue);
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("============================");

            return sb.ToString();
        }
    }
}
