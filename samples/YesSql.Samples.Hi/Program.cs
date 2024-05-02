using System;
using System.Threading.Tasks;
using YesSql.Provider.SqlServer;
using YesSql.Samples.Hi.Indexes;
using YesSql.Samples.Hi.Models;
using YesSql.Sql;

namespace YesSql.Samples.Hi
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            var store = await StoreFactory.CreateAndInitializeAsync(
                new Configuration()
                    .UseSqlServer(@"Data Source =.; Initial Catalog = yessql; Integrated Security = True")
                    .SetTablePrefix("Hi")
                );

            await using (var connection = store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync(store.Configuration.IsolationLevel);
                var builder = new SchemaBuilder(store.Configuration, transaction);

                await builder.CreateMapIndexTableAsync<BlogPostByAuthor>(table => table
                        .Column<string>("Author")
                    );

                await builder.CreateReduceIndexTableAsync<BlogPostByDay>(table => table
                        .Column<int>("Count")
                        .Column<int>("Day")
                );

                await transaction.CommitAsync();
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
            await using (var session = store.CreateSession())
            {
                await session.SaveAsync(post);
                await session.SaveChangesAsync();
            }

            // loading a single blog post
            await using (var session = store.CreateSession())
            {
                var p = await session.Query().For<BlogPost>().FirstOrDefaultAsync();
                Console.WriteLine(p.Title); // > Hello YesSql
            }

            // loading blog posts by author
            await using (var session = store.CreateSession())
            {
                var ps = await session.Query<BlogPost, BlogPostByAuthor>().Where(x => x.Author.StartsWith("B")).ListAsync();

                foreach (var p in ps)
                {
                    Console.WriteLine(p.Author); // > Bill
                }
            }

            // loading blog posts by day of publication
            await using (var session = store.CreateSession())
            {
                var ps = await session.Query<BlogPost, BlogPostByDay>(x => x.Day == DateTime.UtcNow.ToString("yyyyMMdd")).ListAsync();

                foreach (var p in ps)
                {
                    Console.WriteLine(p.PublishedUtc); // > [Now]
                }
            }

            // counting blog posts by day
            await using (var session = store.CreateSession())
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