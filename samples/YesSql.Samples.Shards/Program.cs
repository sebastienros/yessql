using System;
using FluentNHibernate.Cfg.Db;
using NHibernate.Tool.hbm2ddl;
using YesSql.Core.Data;
using YesSql.Samples.Shards.Indexes;
using YesSql.Samples.Shards.Models;

namespace YesSql.Samples.Shards
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var store = new Store().Configure(s => {
                var config0 = s.CreateConfiguration("Shard0", () =>
                    MsSqlConfiguration.MsSql2008
                        .ConnectionString("Server=.;Database=Shard0;Integrated Security = True")
                        );

                var config1 = s.CreateConfiguration("Shard1", () =>
                    MsSqlConfiguration.MsSql2008
                        .ConnectionString("Server=.;Database=Shard1;Integrated Security = True")
                        );

                new SchemaUpdate(config0).Execute(false, true);
                new SchemaUpdate(config1).Execute(false, true);
            })
            .SetShardingStrategy(new ShardStrategyFactory())
            .RegisterIndexes<ProductIndexProvider>()
            .RegisterIndexes<OrderIndexProvider>()
            ;
            
            using(var session = store.CreateSession())
            {
                var product = new Product {
                    Cost = 3.99m,
                    Name = "Milk",
                };

                session.Save(product);
                session.Commit();

                session.Save(new Order {
                    Customer = "Microsoft",
                    OrderLines =
                        {
                            new OrderLine
                            {
                                ProductId = product.Id,
                                Quantity = 3
                            },
                        }
                });

                session.Save(new Order {
                    Customer = "Microsoft",
                    OrderLines =
                        {
                            new OrderLine
                            {
                                ProductId = product.Id,
                                Quantity = 5
                            },
                        }
                });

                session.Commit();
            }

            using(var session = store.CreateSession())
            {
                var p = session.Query<Product, ProductByName>().Where(x => x.Name == "Milk").FirstOrDefault();
                Console.WriteLine(p.Name + " " + p.Cost);

                var orders = session.Query<Order, OrderByCustomerName>().Where(x => x.Name == "Microsoft").List();
                foreach (var order in orders)
                {
                    Console.WriteLine(order.Id);
                }
            }
        }
    }
}
