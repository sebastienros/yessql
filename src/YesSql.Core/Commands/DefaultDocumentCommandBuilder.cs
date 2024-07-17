using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public class DefaultDocumentCommandBuilder : IDocumentCommandBuilder
    {
        public DocumentCommand BuildCreateDocumentCommand(object entity, Document document, IStore store, string collection)
        {
            return new CreateDocumentCommand(document, store, collection);
        }
        public DocumentCommand BuildUpdateDocumentCommand(object entity, Document document, IStore store, long checkVersion, string collection)
        {
            return new UpdateDocumentCommand(document, store, checkVersion, collection);
        }
        public DocumentCommand BuildDeleteDocumentCommand(object entity, Document document, IStore store, string collection)
        {
            return new DeleteDocumentCommand(document, store, collection);
        }
    }
}
