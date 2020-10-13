using System.Collections.Generic;

namespace Perpetuum.DataDumper {
    public class EntityDataView {
        public string item_name { get; set; } // This should actually be renamed...
        public string item_key { get; set; }
        public List<string> item_categories { get; set; }
    }
}
