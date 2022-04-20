using System;
using System.IO;
using System.Threading.Tasks;
using YesSql.Provider.Sqlite;
using YesSql.Samples.Hi.Indexes;
using YesSql.Samples.Hi.Models;
using YesSql.Sql;

namespace YesSql.Samples.Hi
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var filename = "yessql.db";

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            var configuration = new Configuration()
                .UseSqLite($"Data Source={filename};Cache=Shared");
            var store = await StoreFactory.CreateAndInitializeAsync(configuration);

            using (var connection = store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(store.Configuration.IsolationLevel))
                {
                    new SchemaBuilder(store.Configuration, transaction)
                        .CreateMapIndexTable<BlogPostByAuthor>(table => table
                            .Column<string>("Author")
                        )
                        .CreateReduceIndexTable<BlogPostByDay>(table => table
                            .Column<int>("Count")
                            .Column<int>("Day")
                        )
                        .CreateMapIndexTable<BlogPostByTag>(table => table
                            .Column<string>("Tag")
                        );

                    transaction.Commit();
                }
            }

            // register available indexes
            store.RegisterIndexes<BlogPostIndexProvider>();

            // creating a blog post
            var post1 = new BlogPost
            {
                Title = "Hello YesSql",
                Author = "Bill",
                Content = "Hello Bill!",
                PublishedUtc = DateTime.UtcNow,
                Tags = new[] { "Hello", "YesSql", "Test" }
            };
            var post2 = new BlogPost
            {
                Title = "Bye YesSql",
                Author = "Bill",
                Content = "Bye Bill!",
                PublishedUtc = DateTime.UtcNow,
                Tags = new[] { "Bye", "YesSql", "Test" }
            };
            var post3 = new BlogPost
            {
                Title = "Other blog title",
                Author = "Lucian",
                Content = "This is the content.",
                PublishedUtc = DateTime.UtcNow,
                Tags = new[] { "Other", "YesSql", "Test", "Blog" }
            };

            // saving the post to the database
            using (var session = store.CreateSession())
            {
                session.Save(post1);
                session.Save(post2);
                session.Save(post3);

                await session.SaveChangesAsync();
            }

            // loading a single blog post
            using (var session = store.CreateSession())
            {
                var p = await session.Query().For<BlogPost>().FirstOrDefaultAsync();
                Console.WriteLine($"First blog: '{p.Title}'"); // > Hello YesSql
            }
            Console.WriteLine("");

            // loading blog posts by author
            using (var session = store.CreateSession())
            {
                var ps = await session.Query<BlogPost, BlogPostByAuthor>().Where(x => x.Author.StartsWith("B")).ListAsync();

                foreach (var p in ps)
                {
                    Console.WriteLine($"'{p.Title}' by {p.Author}"); // > Bill
                }
            }
            Console.WriteLine("");

            // loading blog posts by day of publication
            using (var session = store.CreateSession())
            {
                var ps = await session.Query<BlogPost, BlogPostByDay>(x => x.Day == DateTime.UtcNow.ToString("yyyyMMdd")).ListAsync();

                foreach (var p in ps)
                {
                    Console.WriteLine($"'{p.Title}' published on {p.PublishedUtc}"); // > [Now]
                }
            }
            Console.WriteLine("");

            // counting blog posts by day
            using (var session = store.CreateSession())
            {
                var days = await session.QueryIndex<BlogPostByDay>().ListAsync();

                foreach (var day in days)
                {
                    Console.WriteLine($"Blogs count on {day.Day}: {day.Count}"); // > [Today]: 1
                }
            }
            Console.WriteLine("");

            // load blog posts with tag "Hello"
            using (var session = store.CreateSession())
            {
                var ps = await session.Query<BlogPost, BlogPostByTag>(x => x.Tag == "Hello").ListAsync();

                foreach (var p in ps)
                {
                    Console.WriteLine($"'{p.Title}' has 'Hello' tag. Tag list: " + string.Join(", ", p.Tags));
                }
            }
            Console.WriteLine("");

            // load blog posts with tag "Test"
            using (var session = store.CreateSession())
            {
                var ps = await session.Query<BlogPost, BlogPostByTag>(x => x.Tag == "Test").ListAsync();

                foreach (var p in ps)
                {
                    Console.WriteLine($"'{p.Title}' has 'Test' tag. Tag list: " + string.Join(", ", p.Tags));
                }
            }
            Console.WriteLine("");
        }
    }
}