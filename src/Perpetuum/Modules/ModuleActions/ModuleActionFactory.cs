using Perpetuum.ExportedTypes;
using Perpetuum.Modules.Weapons;

namespace Perpetuum.Modules.ModuleActions
{
    public class ModuleActionFactory
    {
        public ModuleAction Create(WeaponModule weaponModule)
        {
            CategoryFlags ammoCategory = weaponModule.AmmoCategoryFlags;

            if (ammoCategory.HasFlag(CategoryFlags.cf_artillery_ammo))
            {
                return new ArtilleryModuleAction(weaponModule as ArtilleryWeaponModule);
            }

            return new ModuleAction(weaponModule);
        }
    }
}
