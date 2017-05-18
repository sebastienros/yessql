using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Services;
using YesSql.Sql;
using YesSql.Storage;

namespace YesSql.Storage.Sql
{
    public class SqlDocumentStorage : IDocumentStorage
    {
        private readonly SqlDocumentStorageFactory _factory;
        private readonly ISession _session;

        static SqlDocumentStorage()
        {
            _jsonSettings = 
        }

        public SqlDocumentStorage(ISession session, SqlDocumentStorageFactory factory)
        {
            _session = session;
            _factory = factory;
        }


    }
}
