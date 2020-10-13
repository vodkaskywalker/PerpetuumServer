using System.Collections.Generic;

namespace Perpetuum.DataDumper {
    public partial class DataDumper
    {
        public class ModuleDataView : ItemDataView {
            public string module_tier { get; set; }
            public double module_cpu { get; set; }
            public double module_reactor { get; set; }
            public List<string> module_extensions_required { get; set; }
        }

    }
}
