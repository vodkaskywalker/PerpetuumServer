using Perpetuum.Containers;
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
    public class ModuleERPDataView : ActiveModuleDataView {

        public double module_recovery_chemical { get; set; }
        public double module_recovery_kinetic { get; set; }
        public double module_recovery_seismic { get; set; }
        public double module_recovery_thermal { get; set; }

        public ModuleERPDataView(Module item, DataDumper dumper) {
            dumper.InitModuleView(this, item);

            module_recovery_chemical = item.GetBasePropertyModifier(AggregateField.chemical_damage_to_core_modifier).Value * 100;
            module_recovery_kinetic = item.GetBasePropertyModifier(AggregateField.kinetic_damage_to_core_modifier).Value * 100;
            module_recovery_seismic = item.GetBasePropertyModifier(AggregateField.explosive_damage_to_core_modifier).Value * 100;
            module_recovery_thermal = item.GetBasePropertyModifier(AggregateField.thermal_damage_to_core_modifier).Value * 100;
        }
    }
}
