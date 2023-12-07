using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Modules.ModuleActions;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Zones.Locking.Locks;
using System;
using System.Collections.Generic;

namespace Perpetuum.Modules.Weapons
{
    public class ArtilleryWeaponModule : ActiveModule
    {
        private readonly ArtilleryModuleAction _action;

        public ModuleProperty DamageModifier { get; }

        public ModuleProperty Accuracy { get; }

        public ArtilleryWeaponModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
            _action = new ArtilleryModuleAction(this);
            DamageModifier = new ModuleProperty(this, AggregateField.damage_modifier);
            AddProperty(DamageModifier);
            Accuracy = new ModuleProperty(this, AggregateField.accuracy);
            AddProperty(Accuracy);

            cycleTime.AddEffectModifier(AggregateField.effect_weapon_cycle_time_modifier);
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
            
            _action.DoAction();
        }
    }
}
