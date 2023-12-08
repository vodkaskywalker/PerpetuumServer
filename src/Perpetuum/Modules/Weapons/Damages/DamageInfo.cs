using Perpetuum.Units;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Modules.Weapons.Damages
{
    public sealed class DamageInfo
    {
        private const double CRITICALHIT_MOD = 1.75;
        public double _optimalRange;
        public double _falloff;
        public double _explosionRadius;
        public Unit attacker;
        public Position sourcePosition;
        public IList<Damage> damages = new List<Damage>();
        public IList<Damage> plantDamages = new List<Damage>();

        public double Range
        {
            get { return _optimalRange + _falloff; }
        }

        public bool IsCritical { get; private set; }

        public static IDamageBuilder Builder
        {
            get { return new DamageBuilder(); }
        }

        public DamageInfo()
        {
        }

        public IList<Damage> CalculateDamages(Unit target)
        {
            var result = new List<Damage>();
            var zone = target.Zone;

            if (zone != null && damages != null)
            {
                var criticalHitChance = 0.0;

                if (attacker != null)
                {
                    criticalHitChance = attacker.CriticalHitChance;
                }
                

                var random = FastRandom.NextDouble();

                IsCritical = random <= criticalHitChance;

                var damageModifier = IsCritical ? CRITICALHIT_MOD : 1.0;

                damageModifier *= FastRandom.NextDouble(0.9, 1.1);

                if (_optimalRange > 0.0 && _falloff > 0.0)
                {
                    var distance = sourcePosition.TotalDistance2D(target.CurrentPosition);
                    var range = Range;

                    if (distance > range)
                    {
                        damageModifier = 0.0;
                    }
                    else if (_falloff > 0.0 && distance > _optimalRange && distance <= range)
                    {
                        var x = (distance - _optimalRange) / _falloff;

                        damageModifier *= Math.Cos(x * Math.PI) / 2 + 0.5;
                    }
                }

                if (damageModifier > 0.0)
                {
                    if (_explosionRadius > 0.0)
                    {
                        var tmpDamageMod = target.SignatureRadius / _explosionRadius;

                        if (tmpDamageMod <= 0.0 || tmpDamageMod >= 1.0)
                        {
                            tmpDamageMod = 1.0;
                        }

                        damageModifier *= tmpDamageMod;
                    }

                    result = damages.Where(d => d.type != DamageType.Toxic)
                        .Select(d => new Damage(d.type, d.value * damageModifier))
                        .ToList();
                }
            }

            return result;
        }

        public double CalculatePlantDamages()
        {
            return damages.Sum(d => d.value) + plantDamages.Sum(d => d.value);
        }
    }
}
