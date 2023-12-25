using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Commands;

namespace YesSql
{
    public interface IDocumentEventHandler
    {
        Func<Document, object, Task<IEnumerable<IIndexCommand>>> CreateDocumentHandler { get; set; }
        Func<Document, object, Task<IEnumerable<IIndexCommand>>> DeleteDocumentHandler { get; set; }
        Func<Document, object, Task<IEnumerable<IIndexCommand>>> UpdateDocumentHandler { get; set; }
    }
}
