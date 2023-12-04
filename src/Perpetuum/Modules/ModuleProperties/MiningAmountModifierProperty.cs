using Perpetuum.ExportedTypes;

namespace Perpetuum.Modules.ModuleProperties
{
    public class MiningAmountModifierProperty : ModuleProperty
    {
        private readonly DrillerModule _module;

        public MiningAmountModifierProperty(DrillerModule module) : base(module, AggregateField.mining_amount_modifier)
        {
            _module = module;
            AddEffectModifier(AggregateField.effect_mining_amount_modifier);
        }

        protected override double CalculateValue()
        {
            if (module.ParentRobot == null)
            {
                return 1.0;
            }

            var m = module.ParentRobot.GetPropertyModifier(AggregateField.mining_amount_modifier);
            var ammo = (MiningAmmo)_module.GetAmmo();

            ammo?.ApplyMiningAmountModifier(ref m);
            module.ParentRobot?.ApplyEffectPropertyModifiers(AggregateField.effect_mining_amount_modifier, ref m);

            return m.Value;
        }
    }
}
