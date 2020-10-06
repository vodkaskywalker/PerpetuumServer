using Perpetuum.Containers;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper.Views {
    public class ModuleShieldGeneratorDataView : ActiveModuleDataView {

        public double module_shield_radius { get; set; }
        public double module_absorption_ratio { get; set; }
    }
}
