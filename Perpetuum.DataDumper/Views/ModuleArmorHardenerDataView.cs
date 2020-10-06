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
    public class ModuleArmorHardenerDataView : ActiveModuleDataView {

        public double module_resist_active_chemical { get; set; }
        public double module_resist_passive_chemical { get; set; }
        public double module_resist_active_kinetic { get; set; }
        public double module_resist_passive_kinetic { get; set; }
        public double module_resist_active_seismic { get; set; }
        public double module_resist_passive_seismic { get; set; }
        public double module_resist_active_thermal { get; set; }
        public double module_resist_passive_thermal { get; set; }
        public string module_resist_type { get; set; }
        public double module_resist_passive { get; set; }
        public double module_resist_active { get; set; }
    }
}
