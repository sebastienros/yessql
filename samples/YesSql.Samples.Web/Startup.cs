using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using YesSql.Filters.Query;
using YesSql.Provider.Sqlite;
using YesSql.Samples.Web.Indexes;
using YesSql.Samples.Web.Models;
using YesSql.Samples.Web.ViewModels;
using YesSql.Services;
using YesSql.Sql;

namespace YesSql.Samples.Web
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var filename = "yessql.db";

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            services.AddDbProvider(config =>
                config.UseSqLite($"Data Source={filename};Cache=Shared"));

            services.AddMvc(options => options.EnableEndpointRouting = false);

            services.AddSingleton(sp =>
                new QueryEngineBuilder<BlogPost>()
                    .WithNamedTerm("status", builder => builder
                        .OneCondition((val, query) =>
                        {
                            if (Enum.TryParse<BlogPostStatus>(val, true, out var e))
                            {
                                switch (e)
                                {
                                    case BlogPostStatus.Published:
                                        query.With<BlogPostIndex>(x => x.Published);
                                        break;
                                    case BlogPostStatus.Draft:
                                        query.With<BlogPostIndex>(x => !x.Published);
                                        break;
                                    default:
                                        break;
                                }
                            }

                            return query;
                        })
                        .MapTo<Filter>((val, model) =>
                        {
                            if (Enum.TryParse<BlogPostStatus>(val, true, out var e))
                            {
                                model.SelectedStatus = e;
                            }
                        })
                        .MapFrom<Filter>((model) =>
                        {
                            if (model.SelectedStatus != BlogPostStatus.Default)
                            {
                                return (true, model.SelectedStatus.ToString());
                            }

                            return (false, string.Empty);

                        })
                    )
                    .WithNamedTerm("sort", b => b
                        .OneCondition((val, query) =>
                        {
                            if (Enum.TryParse<BlogPostSort>(val, true, out var e))
                            {
                                switch (e)
                                {
                                    case BlogPostSort.Newest:
                                        query.With<BlogPostIndex>().OrderByDescending(x => x.PublishedUtc);
                                        break;
                                    case BlogPostSort.Oldest:
                                        query.With<BlogPostIndex>().OrderBy(x => x.PublishedUtc);
                                        break;
                                    default:
                                        query.With<BlogPostIndex>().OrderByDescending(x => x.PublishedUtc);
                                        break;
                                }
                            }
                            else
                            {
                                query.With<BlogPostIndex>().OrderByDescending(x => x.PublishedUtc);
                            }

                            return query;
                        })
                        .MapTo<Filter>((val, model) =>
                        {
                            if (Enum.TryParse<BlogPostSort>(val, true, out var e))
                            {
                                model.SelectedSort = e;
                            }
                        })
                        .MapFrom<Filter>((model) =>
                        {
                            if (model.SelectedSort != BlogPostSort.Newest)
                            {
                                return (true, model.SelectedSort.ToString());
                            }

                            return (false, string.Empty);

                        })
                        .AlwaysRun()
                    )
                    .WithDefaultTerm("title", b => b
                        .ManyCondition(
                            ((val, query) => query.With<BlogPostIndex>(x => x.Title.Contains(val))),
                            ((val, query) => query.With<BlogPostIndex>(x => x.Title.IsNotIn<BlogPostIndex>(s => s.Title, w => w.Title.Contains(val))))
                        )
                    )
                    .Build()
            );
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var store = app.ApplicationServices.GetRequiredService<IStore>();
            store.RegisterIndexes(new[] { new BlogPostIndexProvider() });
            Task.Run(async () =>
            {
                await using var connection = store.Configuration.ConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                await using var transaction = connection.BeginTransaction(store.Configuration.IsolationLevel);
                var builder = new SchemaBuilder(store.Configuration, transaction);

                await builder.CreateMapIndexTableAsync<BlogPostIndex>(table => table
                        .Column<string>("Title")
                        .Column<string>("Author")
                        .Column<string>("Content")
                        .Column<DateTime>("PublishedUtc")
                        .Column<bool>("Published")
                    );

                await transaction.CommitAsync();

                await using var session = app.ApplicationServices.GetRequiredService<IStore>().CreateSession();
                await session.SaveAsync(new BlogPost
                {
                    Title = "On the beach in the sand we found lizards",
                    Author = "Steve Balmer",
                    Content = "Steves first post",
                    PublishedUtc = DateTime.UtcNow,
                    Published = false,
                    Tags = Array.Empty<string>()
                });

                await session.SaveAsync(new BlogPost
                {
                    Title = "On the beach in the sand we built sandcastles",
                    Author = "Bill Gates",
                    Content = "Bill first post",
                    PublishedUtc = DateTime.UtcNow,
                    Published = true,
                    Tags = Array.Empty<string>()
                });

                await session.SaveAsync(new BlogPost
                {
                    Title = "On the mountain it snowed at the lake",
                    Author = "Paul Allen",
                    Content = "Pauls first post",
                    PublishedUtc = DateTime.UtcNow,
                    Published = true,
                    Tags = Array.Empty<string>()
                });

                await session.SaveChangesAsync();
            });
        }
    }
}
