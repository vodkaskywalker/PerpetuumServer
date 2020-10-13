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
    public class ModuleArmorPlateDataView : ModuleDataView {
        public double module_hp { get; set; }
        public double module_surface_hit { get; set; }
        public double module_demob_resist { get; set; }

        public ModuleArmorPlateDataView(Module item, DataDumper dumper) {
            dumper.InitModuleView(this, item);

            module_hp = item.GetBasePropertyModifier(AggregateField.armor_max).Value;
            module_demob_resist = item.GetBasePropertyModifier(AggregateField.massiveness).Value * 100;
            module_surface_hit = item.GetBasePropertyModifier(AggregateField.signature_radius).Value;
        }
    }
}
