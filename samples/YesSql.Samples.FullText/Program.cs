using System;
using Microsoft.Data.Sqlite;
using YesSql.Core.Services;
using YesSql.Samples.FullText.Indexes;
using YesSql.Samples.FullText.Models;
using YesSql.Storage.InMemory;
using MySql.Data.MySqlClient;

namespace YesSql.Samples.FullText
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var configuration = new Configuration
            {
                ConnectionFactory = new DbConnectionFactory<MySqlConnection>(@"server=127.0.0.1,3306;uid=root;pwd=920624ppN_;database=dbSample2;", true),
                DocumentStorageFactory = new InMemoryDocumentStorageFactory()
            };

            var store = new Store(configuration);

            store.InitializeAsync().Wait();

            using (var session = store.CreateSession())
            {
                session.ExecuteMigration(x => x
                    .CreateReduceIndexTable(nameof(ArticleByWord), table => table
                        .Column<int>("Count")
                        .Column<string>("Word")
                    )
                );
            }

            // register available indexes
            store.RegisterIndexes<ArticleIndexProvider>();

            // creating articles
            using (var session = store.CreateSession())
            {
                session.Save(new Article { Content = "This is a white fox" });
                session.Save(new Article { Content = "This is a brown cat" });
                session.Save(new Article { Content = "This is a pink elephant" });
                session.Save(new Article { Content = "This is a white tiger" });
            }

            using (var session = store.CreateSession())
            {
                Console.WriteLine("Simple term: 'white'");
                var simple = session.QueryAsync<Article, ArticleByWord>().Where(a => a.Word == "white").List().Result;

                foreach (var article in simple)
                {
                    Console.WriteLine(article.Content);
                }

                Console.WriteLine("Boolean query: 'white or fox or pink'");
                var boolQuery = session.QueryAsync<Article, ArticleByWord>().Where(a => a.Word.IsIn(new[] { "white", "fox", "pink" })).List().Result;

                foreach (var article in boolQuery)
                {
                    Console.WriteLine(article.Content);
                }
            }
        }
    }
}