using Perpetuum.ExportedTypes;
using Perpetuum.Items.Ammos;
using Perpetuum.Items;
using System.Collections.Generic;
using System.Threading;

namespace Perpetuum.Modules.Weapons
{
    public class ArtilleryWeaponAmmo : Ammo
    {
        private ItemProperty _optimalRangeModifier = ItemProperty.None;
        private IList<Damage> _cleanDamages;

        public override void UpdateAllProperties()
        {
            base.UpdateAllProperties();
            UpdateCleanDamages();
        }

        public IList<Damage> GetCleanDamages()
        {
            return LazyInitializer.EnsureInitialized(ref _cleanDamages, CalculateCleanDamages);
        }

        public override void ModifyOptimalRange(ref ItemPropertyModifier property)
        {
            var optimalRangeMod = _optimalRangeModifier.ToPropertyModifier();

            optimalRangeMod.Modify(ref property);
        }

        public ItemPropertyModifier GetExplosionRadius()
        {
            return GetPropertyModifier(AggregateField.explosion_radius);
        }

        private IList<Damage> CalculateCleanDamages()
        {
            var result = new List<Damage>();

            if (!(GetParentModule() is ArtilleryWeaponModule weapon))
            {
                return result;
            }

            var damageModifier = weapon.DamageModifier.ToPropertyModifier();
            var property = GetPropertyModifier(AggregateField.damage_chemical);

            if (property.HasValue)
            {
                damageModifier.Modify(ref property);
                result.Add(new Damage(DamageType.Chemical, property.Value));
            }

            property = GetPropertyModifier(AggregateField.damage_thermal);

            if (property.HasValue)
            {
                damageModifier.Modify(ref property);
                result.Add(new Damage(DamageType.Thermal, property.Value));
            }

            property = GetPropertyModifier(AggregateField.damage_kinetic);

            if (property.HasValue)
            {
                damageModifier.Modify(ref property);
                result.Add(new Damage(DamageType.Kinetic, property.Value));
            }

            property = GetPropertyModifier(AggregateField.damage_explosive);

            if (!property.HasValue)
            {
                return result;
            }

            damageModifier.Modify(ref property);
            result.Add(new Damage(DamageType.Explosive, property.Value));

            return result;
        }

        private void UpdateCleanDamages()
        {
            _cleanDamages = null;
        }
    }
}
