using System;
using System.Threading.Tasks;
using YesSql.Provider.SqlServer;
using YesSql.Samples.Hi.Indexes;
using YesSql.Samples.Hi.Models;
using YesSql.Sql;

namespace YesSql.Samples.Hi
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
                    .SetTablePrefix("Hi")
                );

            using (var connection = store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(store.Configuration.IsolationLevel))
                {
                    new SchemaBuilder(store.Configuration, transaction)
                        .CreateMapIndexTable(nameof(BlogPostByAuthor), table => table
                            .Column<string>("Author")
                        )
                        .CreateReduceIndexTable(nameof(BlogPostByDay), table => table
                            .Column<int>("Count")
                            .Column<int>("Day")
                    );

                    transaction.Commit();
                }
            };

            // register available indexes
            store.RegisterIndexes<BlogPostIndexProvider>();

            // creating a blog post
            var post = new BlogPost
            {
                Title = "Hello YesSql",
                Author = "Bill",
                Content = "Hello",
                PublishedUtc = DateTime.UtcNow,
                Tags = new[] { "Hello", "YesSql" }
            };

            // saving the post to the database
            using (var session = store.CreateSession())
            {
                session.Save(post);
            }

            // loading a single blog post
            using (var session = store.CreateSession())
            {
                var p = await session.Query().For<BlogPost>().FirstOrDefaultAsync();
                Console.WriteLine(p.Title); // > Hello YesSql
            }

            // loading blog posts by author
            using (var session = store.CreateSession())
            {
                var ps = await session.Query<BlogPost, BlogPostByAuthor>().Where(x => x.Author.StartsWith("B")).ListAsync();

                foreach (var p in ps)
                {
                    Console.WriteLine(p.Author); // > Bill
                }
            }

            // loading blog posts by day of publication
            using (var session = store.CreateSession())
            {
                var ps = await session.Query<BlogPost, BlogPostByDay>(x => x.Day == DateTime.UtcNow.ToString("yyyyMMdd")).ListAsync();

                foreach (var p in ps)
                {
                    Console.WriteLine(p.PublishedUtc); // > [Now]
                }
            }

            // counting blog posts by day
            using (var session = store.CreateSession())
            {
                var days = await session.QueryIndex<BlogPostByDay>().ListAsync();

                foreach (var day in days)
                {
                    Console.WriteLine(day.Day + ": " + day.Count); // > [Today]: 1
                }
            }
        }
    }
}