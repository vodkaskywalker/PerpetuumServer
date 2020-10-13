namespace Perpetuum.DataDumper {
    public partial class DataDumper
    {
        public class ActiveModuleDataView : ModuleDataView {
            // These are nullable because some items may be
            // in a group of active modules but are themselves
            // passive and we don't want to show 0 for them
            public double? module_accumulator { get; set; }
            public double? module_cycle { get; set; }

        }

    }
}
