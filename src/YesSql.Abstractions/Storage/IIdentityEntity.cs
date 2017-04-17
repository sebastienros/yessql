using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YesSql.Storage
{
    public interface IIdentityEntity
    {
        int Id { get; set; }
        object Entity { get; set; }
        Type EntityType { get; set; }
    }
}
