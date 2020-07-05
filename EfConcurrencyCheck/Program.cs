using Microsoft.EntityFrameworkCore;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EfConcurrencyCheck
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (var db = new TestDbContext())
            {
                await db.Database.EnsureDeletedAsync();
                await db.Database.MigrateAsync();

                db.People.RemoveRange(db.People);

                db.People.Add(new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                    Age = 18
                });

                await db.SaveChangesAsync();
            }

            // ** The document is at version 1 **

            var user_b_saved = new ManualResetEventSlim(false);

            var user_a = Task.Run(async () =>
            {
                PersonViewModel vm;

                {
                    // 1. User-A creates a Session-A, loads the document, populates the web form, and then the Session - A is disposed. The request is complete.
                    using var db = new TestDbContext();
                    var entity = await db.People.FirstOrDefaultAsync();

                    vm = new PersonViewModel(entity);  // ** The entity's RowVersion is captured in the view model right here.
                }

                // person is now being editing by User-A in a web form.
                vm.Firstname = "William";
                vm.Anonymous = false;

                user_b_saved.Wait();

                {
                    // 4. User-A submits form with Values-B two minutes later, creates Session-D, updates the documents, saves, and then Session-D is disposed. The request is complete.
                    using var db = new TestDbContext();

                    var entity = await db.People.FirstOrDefaultAsync();

                    entity.Firstname = vm.Firstname;
                    entity.Lastname = vm.Lastname;
                    entity.Age = vm.Age;
                    entity.Anonymous = vm.Anonymous;

                    // ** Specify the expected Version right here to use in a concurrency check.
                    var entry = db.Entry(entity);                    
                    var version = entry.Property(o => o.Version);                    
                    version.OriginalValue = vm.Version;

                    // You could also do this, which still isn't possible with YesSql because you can't capture the Document.Version anywhere.
                    // if (!entity.Version.SequenceEqual(vm.Version))
                    // {
                    //     throw new Exception("The entity was modified by someone else.");
                    // }

                    try
                    {
                        await db.SaveChangesAsync();
                    }
                    catch (Exception)
                    {
                        // Put a breakpoint here, and we'll hit it every time since
                        // DB.Version != VM.Version
                        throw;
                    }
                }
            });

            var user_b = Task.Run(async () =>
            {
                PersonViewModel vm;

                {
                    // 2. User-B creates a Session-B, loads the document, populates the web form, and then the Session-C is disposed. The request is complete.
                    using var db = new TestDbContext();
                    var entity = await db.People.FirstOrDefaultAsync();

                    vm = new PersonViewModel(entity);  // ** The entity's RowVersion is captured in the view model right here.
                }

                // person is now being editing by User-B in a web form.
                vm.Age = 13;
                vm.Anonymous = true;

                {
                    // 4. User-B submits form with Values-A, creates Session-C, updates the documents, saves, and then Session-C is disposed. The request is complete.
                    using var db = new TestDbContext();

                    var entity = await db.People.FirstOrDefaultAsync();

                    entity.Firstname = vm.Firstname;
                    entity.Lastname = vm.Lastname;
                    entity.Age = vm.Age;
                    entity.Anonymous = vm.Anonymous;

                    // ** Specify the expected Version right here to use in a concurrency check.
                    var entry = db.Entry(entity);
                    var version = entry.Property(o => o.Version);
                    version.OriginalValue = vm.Version;

                    try
                    {
                        await db.SaveChangesAsync();
                    }
                    catch (Exception)
                    {
                        // Put a breakpoint here, and we'll never hit it since
                        // DB.Version = VM.Version
                        throw;
                    }
                }

                user_b_saved.Set();
            });

            await Task.WhenAll(user_a, user_b);
        }
    }

    public class PersonViewModel
    {
        public PersonViewModel(Person person)
        {
            Id = person.Id;
            Firstname = person.Firstname;
            Lastname = person.Lastname;
            Age = person.Age;
            Anonymous = person.Anonymous;
            Version = person.Version;
        }

        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public int Age { get; set; }
        public bool Anonymous { get; set; }
        public byte[] Version { get; set; }
    }

    public class TestDbContext : DbContext
    {
        public DbSet<Person> People { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = "(local)",
                InitialCatalog = "PersonTest",
                IntegratedSecurity = true,
                MultipleActiveResultSets = true
            };

            options.UseSqlServer(builder.ToString());
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Person>(entity =>
            {
                entity.Property(o => o.Version).IsRowVersion();
            });
        }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public int Age { get; set; }
        public bool Anonymous { get; set; }
        public byte[] Version { get; set; }
    }
}
