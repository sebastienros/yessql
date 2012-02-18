using System;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using YesSql.Core.Data;
using YesSql.Samples.FullText.Indexes;
using YesSql.Samples.FullText.Models;

namespace YesSql.Samples.FullText
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // configure the store to use a local SqlCe database
            InitializeDatabase();
            var store = new Store().Configure(MsSqlCeConfiguration.MsSqlCe40.ConnectionString("Data Source=Store.sdf"));

            // register available indexes
            store.RegisterIndexes<ArticleIndexProvider>();

            // creating articles
            using (var session = store.CreateSession())
            {
                session.Save(new Article {Content = "This is a white fox"});
                session.Save(new Article {Content = "This is a brown cat"});
                session.Save(new Article {Content = "This is a pink elephant"});
                session.Save(new Article {Content = "This is a white tiger"});
                session.Commit();
            }

            using (var session = store.CreateSession())
            {
                Console.WriteLine("Simple term: 'white'");
                var simple = session.QueryByReducedIndex<ArticleByWord, Article>(
                    q => q.Where(a => a.Word == "white"));

                foreach (var article in simple) {
                    Console.WriteLine(article.Content);
                }

                Console.WriteLine("Boolean query: 'white and fox or pink'");
                var white = session.QueryByReducedIndex<ArticleByWord, Article>(
                    q => q.Where(a => a.Word == "white"));

                var fox = session.QueryByReducedIndex<ArticleByWord, Article>(
                    q => q.Where(a => a.Word == "fox"));

                var pink = session.QueryByReducedIndex<ArticleByWord, Article>(
                    q => q.Where(a => a.Word == "pink"));

                var result = white.Intersect(fox).Union(pink);

                foreach (var article in result)
                {
                    Console.WriteLine(article.Content);
                }
            }
        }

        /// <summary>
        /// Creates a fresh database
        /// </summary>
        private static void InitializeDatabase() {
            // delete the db before starting tests
            if (File.Exists("Store.sdf")) {
                File.Delete("Store.sdf");
            }

            // recreating a fresh SqlCe db
            new SqlCeEngine { LocalConnectionString = "Data Source=Store.sdf" }.CreateDatabase();
        }
    }
}