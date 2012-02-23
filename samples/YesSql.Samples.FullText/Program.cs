using System;
using System.Data.SqlServerCe;
using System.IO;
using NHibernate.Criterion;
using YesSql.Core.Data;
using YesSql.Core.Data.Models;
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
                var simple = session.Query<Article, ArticleByWord>().Where(a => a.Word == "white").List();

                foreach (var article in simple) {
                    Console.WriteLine(article.Content);
                }

                Document document = null;

                Console.WriteLine("Boolean query: 'white or fox or pink'");
                var boolQuery = session.Query<Article, ArticleByWord>().Where(a => a.Word.IsIn(new [] { "white", "fox", "pink" })).List();

                foreach (var article in boolQuery)
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