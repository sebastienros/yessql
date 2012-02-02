YesSql
=============

A .NET document database using any RDBMS, because in SQL we (still) trust !

How it works ?
-------------------

YesSql is a document database which allows you to define documents and indexes using plain old CLR objects. The main difference
with other document databases is that it uses NHibernate and any RDBMS to store them, which gives you all the power of SQL databases
like transactions, replication, reporting, ... But the main advantage might be that there is no magic involved. It's pure SQL !

FAQ
-------------------

### NOSQL is also about sharding and map/reduce

YesSql has support for the too. There is a YesSql.Shards.Demo project in the source code, and you'll see that map/reduce is fully supported by looking at the tests.

### NOSQL are so fast compared to SQL

Well, I don't what fast is, but you can try to run Performace test to ensure it fulfils your needs. Here is the output on my machine using Microsoft SQL Server 2008:

  YesSql Wrote 5,163 documents in 2,157ms: 2.39: docs/ms

  Queried by full name 100*3 times at 430ms
  Queried by partial name 100*3 times at 827ms

This performance test is based on one which was used to compare Redis to RavenDb. You can find the discussion here: http://www.servicestack.net/mythz_blog/?p=474

### How is the database structured ?

There is a global [Document] table per shard. Then each index has it's own table. In the case of a map/reduce index there is also another table to handle the many-to-many relationships between an indexes and documents.

### Dude ! Why another document database ?

I know :/ Well actually I am a big fan of document databases and I am well aware that some like MongoDb and RavenDb are already top-notch ones, but __what if you want a free, transactional docuemtn database__ ?

* MongoDb is not transactional, and some application can't cope with it. RDBMS on the contrary are all transactional. 
* RavenDb (which I am a big fan of) is not free, and also the fact that it's using Lucene as a storage engine. The latter might scare companies which have invested a lot in SQL, trust SQL, and have in-house experts.

So YesSql might be an answer for the developers who face those restrictions. If you don't so please don't spend one more minute on YesSql, it's useless for you.


