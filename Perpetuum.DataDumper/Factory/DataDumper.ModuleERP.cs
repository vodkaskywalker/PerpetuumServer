using Perpetuum.DataDumper.Views;
using Perpetuum.ExportedTypes;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Services.ExtensionService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper {
    public partial class DataDumper {
        public ModuleERPDataView NewModuleERPDataView(Module module) {
            var newView = new ModuleERPDataView();
            InitModuleView(newView, module);

            newView.module_recovery_chemical = module.GetBasePropertyModifier(AggregateField.chemical_damage_to_core_modifier).Value * 100;
            newView.module_recovery_kinetic = module.GetBasePropertyModifier(AggregateField.kinetic_damage_to_core_modifier).Value * 100;
            newView.module_recovery_seismic = module.GetBasePropertyModifier(AggregateField.explosive_damage_to_core_modifier).Value * 100;
            newView.module_recovery_thermal = module.GetBasePropertyModifier(AggregateField.thermal_damage_to_core_modifier).Value * 100;
            // module.GetBasePropertyModifier(AggregateField.heal)

            return newView;
        }
    }
}