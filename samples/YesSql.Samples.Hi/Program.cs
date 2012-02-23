using System;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using NHibernate.Criterion;
using YesSql.Core.Data;
using YesSql.Samples.Hi.Indexes;
using YesSql.Samples.Hi.Models;

namespace YesSql.Samples.Hi
{
    internal class Program
    {
        private static void Main()
        {
            // configure the store to use a local SqlCe database
            InitializeDatabase();
            var store = new Store().Configure(MsSqlCeConfiguration.MsSqlCe40.ConnectionString("Data Source=Store.sdf"));
            
            // register available indexes
            store.RegisterIndexes<BlogPostIndexProvider>();

            // creating a blog post
            var post = new BlogPost
            {
                Title = "Hello YesSql",
                Author = "Bill",
                Content = "Hello",
                PublishedUtc = DateTime.UtcNow,
                Tags = new[] {"Hello", "YesSql"}
            };

            // saving the post to the database
            using(var session = store.CreateSession())
            {
                session.Save(post);
                session.Commit();
            }

            // loading a single blog post
            using(var session = store.CreateSession())
            {
                var p = session.Query<BlogPost>().FirstOrDefault();
                Console.WriteLine(p.Title); // > Hello YesSql
            }

            // loading blog posts by author
            using (var session = store.CreateSession())
            {
                var ps = session.Query<BlogPost, BlogPostByAuthor>().Where(x => x.Author.IsLike("B%")).List();

                foreach (var p in ps)
                {
                    Console.WriteLine(p.Author); // > Bill
                }
            }

            // loading blog posts by day of publication
            using (var session = store.CreateSession())
            {
                var ps = session.Query<BlogPost, BlogPostByDay>(x => x.Day == DateTime.UtcNow.ToString("yyyyMMdd")).List();

                foreach (var p in ps)
                {
                    Console.WriteLine(p.PublishedUtc); // > [Now]
                }
            }

            // counting blog posts by day
            using (var session = store.CreateSession())
            {
                var days = session.QueryIndex<BlogPostByDay>().ToList();

                foreach (var day in days)
                {
                    Console.WriteLine(day.Day + ": " + day.Count); // > [Today]: 1
                }
            }
        }

        /// <summary>
        /// Creates a fresh database
        /// </summary>
        private static void InitializeDatabase()
        {
            // delete the db before starting tests
            if (File.Exists("Store.sdf")) {
                File.Delete("Store.sdf");
            }

            // recreating a fresh SqlCe db
            new SqlCeEngine { LocalConnectionString = "Data Source=Store.sdf" }.CreateDatabase();
        }
    }
}