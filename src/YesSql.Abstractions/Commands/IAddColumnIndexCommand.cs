using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YesSql.Sql.Schema
{
    public interface IAddColumnIndexCommand : ITableCommand
    {
        string IndexName { get; }
    }
}
