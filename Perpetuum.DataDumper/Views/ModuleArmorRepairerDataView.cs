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
    public class ModuleArmorRepairerDataView : ActiveModuleDataView {       
        public double module_repair_amount { get; set; }

        public ModuleArmorRepairerDataView(ArmorRepairModule item, DataDumper dumper) {
            dumper.InitActiveModuleView(this, item);

            module_repair_amount = item.GetBasePropertyModifier(AggregateField.armor_repair_amount).Value;

        }
    }
}
