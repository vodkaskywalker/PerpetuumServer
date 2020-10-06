using Perpetuum.Items.Ammos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper.Views {
    public class SparkDataView : EntityDataView {
        public double? price_purchase { get; set; }
        public double? price_equip { get; set; }
        public double? required_standing { get; set; }
        public int sequence { get; set; }
        public string icon { get; set; }
        public string extensions { get; set; }
        public string alliance { get; set; }
        public string energy_prop { get; set; }
    }
}
