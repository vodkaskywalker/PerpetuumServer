using Perpetuum.ExportedTypes;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper.Views {
    public class ModuleWeaponDataView : ActiveModuleDataView {
        public string ammo_type { get; set; }
        public int ammo_capacity { get; set; }
        public double module_damage { get; set; }
        public double module_falloff { get; set; }
        public double module_hit_dispersion { get; set; }
        public double module_optimal_range { get; set; }
        public string missile_optimal_range { get; set; }
        
        private List<SlotFlags> slot_flag_values;
        public string slot_flags {
            get {
                return String.Join(";", slot_flag_values.Select(x => x.ToString()));
            }
            set {
                slot_flag_values = EnumHelper<SlotFlags>.MaskToList((SlotFlags)Convert.ToInt64(value)).ToList();
            }
        }
        public string slot_type {
            get {
                return slot_flag_values.Intersect(typeFlags).SingleOrDefault().ToString();
            }
        }

        public string slot_size {
            get {
                return slot_flag_values.Intersect(sizeFlags).SingleOrDefault().ToString();
            }
        }

        public string slot_location {
            get {
                return slot_flag_values.Intersect(locationFlags).SingleOrDefault().ToString();
            }
        }

        public ModuleWeaponDataView(WeaponModule item, DataDumper dumper) {
            dumper.InitActiveModuleView(this, item);

            var dictionaryData = item.ToDictionary();

            dictionaryData["ammoCategoryFlags"] = (CategoryFlags)dictionaryData["ammoCategoryFlags"];

            ammo_type = dumper.GetLocalizedName(((CategoryFlags)dictionaryData["ammoCategoryFlags"]).ToString()).Replace(";undefined", "");
            slot_flags = item.ModuleFlag.ToString();
            ammo_capacity = item.AmmoCapacity;
            module_damage = item.DamageModifier.Value * 100;
            module_falloff = item.Properties.Single(x => x.Field == AggregateField.falloff).Value * 10;
            module_hit_dispersion = item.Accuracy.Value;
            module_optimal_range = item.OptimalRange * 10;

            var rangeModifierProperty = item.GetBasePropertyModifier(AggregateField.module_missile_range_modifier);

            if (rangeModifierProperty.HasValue) {
                string directionSymbol = "+";

                if (rangeModifierProperty.Value < 0) {
                    directionSymbol = "-";
                }

                missile_optimal_range = directionSymbol + (rangeModifierProperty.Value - 1) * 100 + "%";

            }
        }

    }
}
