namespace Perpetuum.DataDumper {
    public partial class DataDumper
    {
        public class ItemDataView : EntityDataView {
            public double item_mass { get; set; }
            public double item_volume { get; set; }
            public double item_volume_packed { get; set; }

        }

    }
}
