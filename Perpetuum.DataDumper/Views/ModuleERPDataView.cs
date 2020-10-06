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
    public class ModuleERPDataView : ActiveModuleDataView {

        public double module_recovery_chemical { get; set; }
        public double module_recovery_kinetic { get; set; }
        public double module_recovery_seismic { get; set; }
        public double module_recovery_thermal { get; set; }
    }
}
