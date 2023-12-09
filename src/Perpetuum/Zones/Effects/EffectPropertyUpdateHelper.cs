using Perpetuum.ExportedTypes;
using Perpetuum.Units;
using System.Collections.Generic;

namespace Perpetuum.Zones.Effects
{
    public class EffectPropertyUpdateHelper
    {
        private readonly HashSet<AggregateField> _relatedFields = new HashSet<AggregateField>();

        public void AddEffect(Effect effect)
        {
            foreach (var modifier in effect.PropertyModifiers)
            {
                _relatedFields.Add(modifier.Field);
            }
        }

        public void Update(Unit unit)
        {
            if (_relatedFields.Count == 0)
            {
                return;
            }

            foreach (var modifier in _relatedFields)
            {
                unit.UpdateRelatedProperties(modifier);
            }
        }
    }
}
