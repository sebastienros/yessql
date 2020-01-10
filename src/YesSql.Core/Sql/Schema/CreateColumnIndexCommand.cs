using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YesSql.Sql.Schema
{
    public class CreateColumnIndexCommand : ColumnCommand, ICreateColumnIndexCommand
    {
        public CreateColumnIndexCommand(string tableName, string name) : base(tableName, name) { }
        public bool IsInclude { get; set; }
        public ICreateColumnIndexCommand UseInclude()
        {
            IsInclude = true;
            return this;
        }
        public ICreateColumnIndexCommand WithColumn(string columnName)
        {
            ColumnName = columnName;
            return this;
        }

    }
}
