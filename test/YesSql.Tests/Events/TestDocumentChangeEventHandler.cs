using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Commands;
using YesSql.Provider;

namespace YesSql.Tests.Events
{
    public class TestDocumentChangeEventHandler : DocumentChangedEventHandlerBase
    {
        private readonly IStore _store;
        public TestDocumentChangeEventHandler(IStore store)
        {
            _store = store;
        }

        public override Task<IEnumerable<IExternalCommand>> CreatedAsync(Document document, object entity)
        {
            var cmds = new List<IExternalCommand>
            {
                new ExternalCommand().SetCommand("update " + _store.Configuration.TablePrefix + "Document set ID=@newId;", new { newId = 2 }),
                //The command should be executed only once in batches or in a single command
                new ExternalCommand()
                            .SetCommand("update " + _store.Configuration.TablePrefix  + "Document set ID=@newId;", new { newId = 5 })
                            .SetBatchCommand("update " + _store.Configuration.TablePrefix  + "Document set ID=ID+10;")
        };

            return Task.FromResult(cmds.AsEnumerable());
        }
    }
}
