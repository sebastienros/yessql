using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YesSql.Sql.Schema
{
    public interface ICreateColumnIndexCommand : ITableCommand
    {
        bool IsInclude { get; }
        string ColumnName { get; }
        ICreateColumnIndexCommand WithColumn(string name);
        ICreateColumnIndexCommand UseInclude();
    }
}
