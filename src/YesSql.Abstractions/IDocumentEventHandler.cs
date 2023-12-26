using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Commands;

namespace YesSql
{
    public interface IDocumentEventHandler
    {
        Func<Document, object, Task<IEnumerable<IExternalCommand>>> CreateDocumentHandler { get; set; }
        Func<Document, object, Task<IEnumerable<IExternalCommand>>> DeleteDocumentHandler { get; set; }
        Func<Document, object, Task<IEnumerable<IExternalCommand>>> UpdateDocumentHandler { get; set; }
    }
}
