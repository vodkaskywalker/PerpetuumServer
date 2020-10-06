using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.DataDumper {
    public class DataExportMapping {
        public string TableName { get; set; }
        public Type ViewType { get; set; }
        public string Category { get; set; }

        public DataExportMapping(string _tableName, Type _type, string _category) {
            TableName = _tableName;
            ViewType = _type;
            Category = _category;
        }

        public override string ToString() {
            return ViewType.Name.ToString();
        }

    }
}
