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
    public class ModuleDemobilizerDataView : ActiveModuleDataView {
        public double ModuleTopSpeedModifier { get; set; }

        public ModuleDemobilizerDataView(ActiveModule item, DataDumper dumper) {
            dumper.InitActiveModuleView(this, item);

            ModuleTopSpeedModifier = item.GetBasePropertyModifier(AggregateField.effect_massivness_speed_max_modifier).Value * 100;
        }
    }
}
