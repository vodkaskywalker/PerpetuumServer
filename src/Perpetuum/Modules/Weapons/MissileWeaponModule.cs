using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Modules.Weapons.Damages;
using Perpetuum.Units;

namespace Perpetuum.Modules.Weapons
{
    public class MissileWeaponModule : WeaponModule
    {
        private readonly ItemProperty _propertyExplosionRadius;
        public readonly ModuleProperty MissileRangeModifier;
        public readonly ModuleProperty MissileFalloffModifier;

        public MissileWeaponModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags)
        {
            _propertyExplosionRadius = new ExplosionRadiusProperty(this);
            AddProperty(_propertyExplosionRadius);
            MissileRangeModifier = new ModuleProperty(this, AggregateField.module_missile_range_modifier);
            MissileRangeModifier.AddEffectModifier(AggregateField.effect_missile_range_modifier);
            AddProperty(MissileRangeModifier);
            MissileFalloffModifier = new ModuleProperty(this, AggregateField.module_missile_falloff_modifier);
            AddProperty(MissileFalloffModifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void UpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.explosion_radius:
                case AggregateField.explosion_radius_modifier:
                    {
                        _propertyExplosionRadius.Update();
                        return;
                    }
                case AggregateField.module_missile_range_modifier:
                case AggregateField.effect_missile_range_modifier:
                    {
                        MissileRangeModifier.Update();
                        return;
                    }
                case AggregateField.module_missile_falloff_modifier:
                    {
                        MissileFalloffModifier.Update();
                        return;
                    }
            }

            base.UpdateProperty(field);
        }

        public override bool CheckAccuracy(Unit victim)
        {
            var rnd = FastRandom.NextDouble();
            var isMiss = rnd > ParentRobot.MissileHitChance;
            return isMiss;
        }

        public override IDamageBuilder GetDamageBuilder()
        {
            return base.GetDamageBuilder().WithExplosionRadius(_propertyExplosionRadius.Value);
        }
    }
}