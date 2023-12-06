using Perpetuum.ExportedTypes;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.Modules.ModuleProperties
{
    public class HarvestingAmountModifierProperty : ModuleProperty
    {
        public HarvestingAmountModifierProperty(GathererModule module) : base(module, AggregateField.harvesting_amount_modifier)
        {
            AddEffectModifier(AggregateField.effect_harvesting_amount_modifier);
        }

        public double GetValueByPlantType(PlantType plantType)
        {
            var modifier = AggregateField.undefined;

            switch (plantType)
            {
                case PlantType.RustBush:
                    {
                        modifier = AggregateField.harvesting_amount_helioptris_modifier;

                        break;
                    }
                case PlantType.SlimeRoot:
                    {
                        modifier = AggregateField.harvesting_amount_triandlus_modifier;

                        break;
                    }
                case PlantType.ElectroPlant:
                    {
                        modifier = AggregateField.harvesting_amount_electroplant_modifier;

                        break;
                    }
                case PlantType.TreeIron:
                    {
                        modifier = AggregateField.harvesting_amount_prismocitae_modifier;

                        break;
                    }
            }

            var property = this.ToPropertyModifier();

            if (module.ParentRobot != null && modifier != AggregateField.undefined)
            {
                var mod = module.ParentRobot.GetPropertyModifier(modifier);

                mod.Modify(ref property);
            }

            return property.Value;
        }

        protected override double CalculateValue()
        {
            if (module.ParentRobot == null)
            {
                return 1.0;
            }

            var p = module.ParentRobot.GetPropertyModifier(AggregateField.harvesting_amount_modifier);

            ApplyEffectModifiers(ref p);

            return p.Value;
        }
    }
}
