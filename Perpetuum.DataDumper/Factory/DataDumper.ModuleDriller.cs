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
        public ModuleDrillerDataView NewModuleDrillerDataView(DrillerModule module) {
            var newView = new ModuleDrillerDataView();
            InitActiveModuleView(newView, module);

            newView.module_mining_modifier = module.GetBasePropertyModifier(AggregateField.mining_amount_modifier).Value;

            return newView;
        }
    }
}