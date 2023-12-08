using Perpetuum.Units;
using System.Collections.Generic;

namespace Perpetuum.Modules.Weapons.Damages
{
    public class DamageBuilder : IDamageBuilder
    {
        private readonly IList<Damage> _damages = new List<Damage>();
        private readonly IList<Damage> _plantDamages = new List<Damage>();
        private Unit _attacker;
        private Position _sourcePosition;
        private double _optimalRange;
        private double _falloff;
        private double _explosionRadius;

        public IDamageBuilder WithAttacker(Unit attacker)
        {
            _attacker = attacker;

            if (attacker != null)
            {
                WithSourcePosition(attacker.PositionWithHeight);
            }

            return this;
        }

        public IDamageBuilder WithAttackerWithoutPosition(Unit attacker)
        {
            _attacker = attacker;

            return this;
        }

        public IDamageBuilder WithSourcePosition(Position position)
        {
            _sourcePosition = position;

            return this;
        }

        public IDamageBuilder WithOptimalRange(double optimalRange)
        {
            _optimalRange = optimalRange;

            return this;
        }

        public IDamageBuilder WithFalloff(double falloff)
        {
            _falloff = falloff;

            return this;
        }

        public IDamageBuilder WithExplosionRadius(double explosionRadius)
        {
            _explosionRadius = explosionRadius;

            return this;
        }

        public IDamageBuilder WithDamage(Damage damage)
        {
            if (damage.type == DamageType.Toxic)
            {
                _plantDamages.Add(damage);
            }
            else
            {
                _damages.Add(damage);
            }

            return this;
        }

        public DamageInfo Build()
        {
            var damageInfo = new DamageInfo
            {
                attacker = _attacker,
                sourcePosition = _sourcePosition,
                damages = _damages,
                plantDamages = _plantDamages,
                _optimalRange = _optimalRange,
                _falloff = _falloff,
                _explosionRadius = _explosionRadius
            };

            return damageInfo;
        }
    }
}
