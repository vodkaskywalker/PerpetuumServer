using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper.Views {
    public class ModuleWeaponDataView : ItemDataView {
        public double cpu { get; set; }
        public double reactor { get; set; }
        public string ammo_type { get; set; }
        public string slot_status { get; set; }

        public int ammo_capacity { get; set; }

        public string module_tier { get; set; }

        public double module_accumulator { get; set; }
        public double module_cycle { get; set; }
        public double module_damage { get; set; }
        public double module_falloff { get; set; }
        public double module_hit_dispersion { get; set; }
        public double module_optimal_range { get; set; }
        public string missile_optimal_range { get; set; }
        public string module_extensions_required { get; set; }

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

    }
}
