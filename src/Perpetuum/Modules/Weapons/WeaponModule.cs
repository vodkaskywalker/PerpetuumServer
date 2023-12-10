using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Modules.ModuleActions;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Modules.Weapons.Amunition;
using Perpetuum.Modules.Weapons.Damages;
using Perpetuum.Units;

namespace Perpetuum.Modules.Weapons
{
    public class WeaponModule : ActiveModule
    {
        private readonly ModuleAction _action;

        public ModuleProperty DamageModifier { get; }

        public ModuleProperty RemoteControlDamageModifier { get; }

        public ModuleProperty Accuracy { get; }

        public WeaponModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
            _action = new ModuleActionFactory().Create(this);
            DamageModifier = new ModuleProperty(this,AggregateField.damage_modifier);
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

        public virtual bool CheckAccuracy(Unit victim)
        {
            var rnd = FastRandom.NextDouble();
            var isMiss = rnd * Accuracy.Value > victim.SignatureRadius;

            return isMiss;
        }

        protected override void OnAction()
        {
            _action.DoAction();
        }

        public virtual IDamageBuilder GetDamageBuilder()
        {
            return DamageInfo.Builder
                .WithAttacker(ParentRobot)
                .WithOptimalRange(OptimalRange)
                .WithFalloff(Falloff)
                .WithDamages(GetCleanDamages());
        }

        private IEnumerable<Damage> GetCleanDamages()
        {
            var ammo = (WeaponAmmo)GetAmmo();

            return ammo != null ? ammo.GetCleanDamages() : new Damage[0];
        }
    }
}