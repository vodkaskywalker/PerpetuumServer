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
    public class ModuleRemoteArmorRepairerDataView : ActiveModuleDataView {

        public double module_repair_amount { get; set; }
        public double module_optimal_range { get; set; }
    }
}
