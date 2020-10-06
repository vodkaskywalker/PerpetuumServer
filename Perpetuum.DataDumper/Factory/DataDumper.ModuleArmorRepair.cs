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
        public ModuleArmorRepairerDataView NewModuleArmorRepairDataView(ArmorRepairModule module) {
            var newView = new ModuleArmorRepairerDataView();
            InitItemView(newView, module);

            var dictionaryData = module.ToDictionary();

            // Now we are ready to map up the data
            newView.module_tier = module.ED.GameTierString();
            newView.cpu = module.CpuUsage;
            newView.reactor = module.PowerGridUsage;
            newView.module_accumulator = module.CoreUsage;
            newView.module_cycle = module.CycleTime.TotalSeconds;
            newView.module_repair_amount = module.GetBasePropertyModifier(AggregateField.armor_repair_amount).Value;

            foreach (var extension in module.ED.EnablerExtensions.Keys) {
                newView.module_extensions_required += GetLocalizedName(extensionReader.GetExtensionName(extension.id)) + "(" + extension.level + ");";
            }

            return newView;
        }
    }
}