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
        public ModuleRemoteArmorRepairerDataView NewModuleRemoteArmorRepairerDataView(ActiveModule module) {
            var newView = new ModuleRemoteArmorRepairerDataView();
            InitActiveModuleView(newView, module);

            newView.module_optimal_range = module.GetBasePropertyModifier(AggregateField.optimal_range).Value * 10;
            newView.module_repair_amount = module.GetBasePropertyModifier(AggregateField.armor_repair_amount).Value;
            // module.GetBasePropertyModifier(AggregateField.heal)

            return newView;
        }
    }
}