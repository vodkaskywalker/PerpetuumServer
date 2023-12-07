using Perpetuum.ExportedTypes;
using Perpetuum.Modules.Weapons;

namespace Perpetuum.Modules.ModuleProperties
{
    public class ExplosionRadiusProperty : ModuleProperty
    {
        private readonly MissileWeaponModule _module;

        public ExplosionRadiusProperty(MissileWeaponModule module) : base(module, AggregateField.explosion_radius)
        {
            _module = module;
        }

        protected override double CalculateValue()
        {
            var ammo = (WeaponAmmo)_module.GetAmmo();

            if (ammo == null)
            {
                return 0.0;
            }

            var property = ammo.GetExplosionRadius();

            _module.ApplyRobotPropertyModifiers(ref property);

            return property.Value;
        }
    }
}
