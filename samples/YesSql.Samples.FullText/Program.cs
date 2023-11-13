using System;
using System.IO;
using System.Threading.Tasks;
using YesSql.Provider.Sqlite;
using YesSql.Samples.FullText.Indexes;
using YesSql.Samples.FullText.Models;
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

            var store = await StoreFactory.CreateAndInitializeAsync(configuration);

            using (var connection = store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(store.Configuration, transaction);

                    await builder.CreateReduceIndexTableAsync<ArticleByWord>(table => table
                        .Column<int>("Count")
                        .Column<string>("Word")
                    );

                    await transaction.CommitAsync();
                }
            }

            // register available indexes
            store.RegisterIndexes<ArticleIndexProvider>();

            // creating articles
            using (var session = store.CreateSession())
            {
                session.Save(new Article { Content = "This is a green fox" });
                session.Save(new Article { Content = "This is a yellow cat" });
                session.Save(new Article { Content = "This is a pink elephant" });
                session.Save(new Article { Content = "This is a green tiger" });

                await session.SaveChangesAsync();
            }

            using (var session = store.CreateSession())
            {
                Console.WriteLine("Simple term: 'green'");
                var simple = await session
                    .Query<Article, ArticleByWord>(x => x.Word == "green")
                    .ListAsync();

                foreach (var article in simple)
                {
                    Console.WriteLine(article.Content);
                }

                Console.WriteLine("Boolean query: 'pink or (green and fox)'");
                var boolQuery = await session.Query<Article>()
                    .Any(
                        x => x.With<ArticleByWord>(a => a.Word == "pink"),
                        x => x.All(
                            x => x.With<ArticleByWord>(a => a.Word == "green"),
                            x => x.With<ArticleByWord>(a => a.Word == "fox")
                        )
                    ).ListAsync();

                foreach (var article in boolQuery)
                {
                    Console.WriteLine(article.Content);
                }
            }
        }
    }
}