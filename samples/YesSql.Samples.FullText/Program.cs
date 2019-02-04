using System;
using System.Threading.Tasks;
using YesSql.Provider.SqlServer;
using YesSql.Samples.FullText.Indexes;
using YesSql.Samples.FullText.Models;
using YesSql.Services;
using YesSql.Sql;

namespace YesSql.Samples.FullText
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            var store = await StoreFactory.CreateAsync(
                new Configuration()
                    .UseSqlServer(@"Data Source =.; Initial Catalog = yessql; Integrated Security = True")
                    .SetTablePrefix("FullText")
                );

            using (var connection = store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(store.Configuration.IsolationLevel))
                {
                    new SchemaBuilder(store.Configuration, transaction)
                        .CreateReduceIndexTable(nameof(ArticleByWord), table => table
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

                Console.WriteLine("Boolean query: 'white or fox or pink'");
                var boolQuery = await session.Query<Article, ArticleByWord>().Where(a => a.Word.IsIn(new[] { "white", "fox", "pink" })).ListAsync();

                foreach (var article in boolQuery)
                {
                    Console.WriteLine(article.Content);
                }
            }
        }
    }
}