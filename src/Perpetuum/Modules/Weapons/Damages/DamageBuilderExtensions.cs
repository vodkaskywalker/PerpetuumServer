using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Perpetuum.Modules.Weapons.Damages
{
    public static class DamageBuilderExtensions
    {
        public static IDamageBuilder WithAllDamageTypes(this IDamageBuilder builder, double damage)
        {
            return builder
                .WithDamage(DamageType.Chemical, damage)
                .WithDamage(DamageType.Explosive, damage)
                .WithDamage(DamageType.Kinetic, damage)
                .WithDamage(DamageType.Thermal, damage);
        }

        public static IDamageBuilder WithDamages(this IDamageBuilder builder, IEnumerable<Damage> damages)
        {
            foreach (var damage in damages)
            {
                builder.WithDamage(damage);
            }

            return builder;
        }

        public static IDamageBuilder WithDamage(this IDamageBuilder builder, DamageType type, double damage)
        {
            Debug.Assert(!double.IsNaN(damage));

            return Math.Abs(damage - 0.0) < double.Epsilon
                ? builder
                : builder.WithDamage(new Damage(type, damage));
        }
    }
}
