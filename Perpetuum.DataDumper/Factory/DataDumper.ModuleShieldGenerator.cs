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
        public ModuleShieldGeneratorDataView NewModuleShieldGeneratorDataView(ActiveModule module) {
            var newView = new ModuleShieldGeneratorDataView();
            InitActiveModuleView(newView, module);

            newView.module_shield_radius = module.GetBasePropertyModifier(AggregateField.shield_radius).Value;
            newView.module_absorption_ratio = Math.Round(1 / module.GetBasePropertyModifier(AggregateField.shield_absorbtion).Value, 3);

            return newView;
        }
    }
}