using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using YesSql;
using YesSql.Filters.Query;
using YesSql.Provider.Sqlite;
using YesSql.Samples.Web.Indexes;
using YesSql.Samples.Web.Models;
using YesSql.Samples.Web.ViewModels;
using YesSql.Services;
using YesSql.Sql;

var builder = WebApplication.CreateBuilder(args);

var filename = "yessql.db";

if (File.Exists(filename))
{
    File.Delete(filename);
}

builder.Services.AddDbProvider(config =>
    config.UseSqLite($"Data Source={filename};Cache=Shared"));

builder.Services.AddMvc(options => options.EnableEndpointRouting = false);

builder.Services.AddSingleton(sp =>
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

var app = builder.Build();

app.UseRouting()
    .UseEndpoints(endpoints =>
    {
        endpoints.MapDefaultControllerRoute();
    });

var store = app.Services.GetRequiredService<IStore>();
store.RegisterIndexes(new[] { new BlogPostIndexProvider() });

await using var connection = store.Configuration.ConnectionFactory.CreateConnection();
await connection.OpenAsync();

await using var transaction = connection.BeginTransaction(store.Configuration.IsolationLevel);
var schemaBuilder = new SchemaBuilder(store.Configuration, transaction);

await schemaBuilder.CreateMapIndexTableAsync<BlogPostIndex>(table => table
        .Column<string>("Title")
        .Column<string>("Author")
        .Column<string>("Content")
        .Column<DateTime>("PublishedUtc")
        .Column<bool>("Published")
    );

await transaction.CommitAsync();

await using var session = app.Services.GetRequiredService<IStore>().CreateSession();
await session.SaveAsync(new BlogPost
{
    Title = "On the beach in the sand we found lizards",
    Author = "Steve Balmer",
    Content = "Steve's first post",
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
    Content = "Paul's first post",
    PublishedUtc = DateTime.UtcNow,
    Published = true,
    Tags = Array.Empty<string>()
});

await session.SaveChangesAsync();
await connection.CloseAsync();

await app.RunAsync();
