using System;
using System.IO;
using System.Threading.Tasks;
using YesSql.Provider.Sqlite;
using YesSql.Samples.FullText.Indexes;
using YesSql.Samples.FullText.Models;
using YesSql.Services;
using YesSql.Sql;

namespace YesSql.Samples.FullText
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var filename = "yessql.db";

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            var configuration = new Configuration()
                .UseSqLite($"Data Source={filename};Cache=Shared")
                ;

            var store = await StoreFactory.CreateAsync(configuration);

            using (var connection = store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(store.Configuration.IsolationLevel))
                {
                    new SchemaBuilder(store.Configuration, transaction)
                        .CreateReduceIndexTable<ArticleByWord>(table => table
                            .Column<int>("Count")
                            .Column<string>("Word")
                        );

                    transaction.Commit();
                }
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
                var simple = await session.Query<Article, ArticleByWord>().Where(a => a.Word == "white").ListAsync();

                foreach (var article in simple)

                {
                    Console.WriteLine(article.Content);
                }

                Console.WriteLine("Boolean query: 'white or brown'");
                var boolQuery = await session.Query<Article, ArticleByWord>()
                    .Where(a => a.Word.IsIn(new[] { "white" }))
                    .Or()
                    .Where(a => a.Word.IsIn(new[] { "brown" }))
                    .ListAsync();

                foreach (var article in boolQuery)
                {
                    Console.WriteLine(article.Content);
                }
            }
        }
    }
}