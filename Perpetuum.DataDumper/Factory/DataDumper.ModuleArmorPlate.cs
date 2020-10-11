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
        public ModuleArmorPlateDataView NewModuleArmorPlateDataView(Module module) {
            var newView = new ModuleArmorPlateDataView();
            InitModuleView(newView, module);

            newView.module_hp = module.GetBasePropertyModifier(AggregateField.armor_max).Value;
            newView.module_demob_resist = module.GetBasePropertyModifier(AggregateField.massiveness).Value * 100;
            newView.module_surface_hit = module.GetBasePropertyModifier(AggregateField.signature_radius).Value;

            return newView;
        }
    }
}