using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Modules.Weapons
{
    public class ArtilleryWeaponModule : WeaponModule
    {
        public ArtilleryWeaponModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags)
        {
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        protected override void OnAction()
        {
            var myLock = GetLock();

            if (myLock is TerrainLock)
            {
                OnError(ErrorCodes.InvalidLockType);

                return;
            }
            
            base.OnAction();
        }
    }
}
