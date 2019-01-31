YesSql
=============

A .NET document database interface for relational databases, because in SQL we (still) trust !

[![Build status](https://ci.appveyor.com/api/projects/status/38x1yf40wbefpvo5?svg=true)](https://ci.appveyor.com/project/SebastienRos/yessql-un1yf)
[![NuGet](https://img.shields.io/nuget/v/YesSql.core.svg)](https://www.nuget.org/packages/YesSql.Core)
[![MyGet](https://img.shields.io/myget/yessql/v/yessql.core.svg)](https://www.myget.org/feed/Packages/yessql)

How does it work ?
-------------------

YesSql is a .NET Core document database interface over relational databases which allows you to define documents and indexes using plain old CLR objects. The main difference
with document databases is that it uses any RDBMS to store them, which gives you all the power of SQL databases
like transactions, replication, reporting, ... But the main advantage might be that there is no magic involved, it's pure SQL !

A video about YesSql was recorded and is available here https://www.youtube.com/watch?v=D42eK6CJjF4 

FAQ
-------------------

### Aren't NoSQL databases also about map/reduce ?

YesSql has support for it too. There is a [sample project](https://github.com/sebastienros/yessql/tree/master/samples/YesSql.Samples.Hi) in the source code, and you'll see that map/reduce is fully supported by looking at the tests.

### Aren't NoSQL databases  faster than SQL databases ?

Well, I don't know what fast is, but you can try to run the [performance sample](https://github.com/sebastienros/yessql/tree/master/samples/YesSql.Samples.Performance) to ensure it fits your needs. Here is the output on my machine using Microsoft SQL Server 2016:

    YesSql Wrote 5,163 documents in 2,157ms: 2.39: docs/ms
  
    Queried by full name 100*3 times at 430ms
    Queried by partial name 100*3 times at 827ms

This performance test is based on one used to compare Redis to RavenDb that you can find here: http://www.servicestack.net/mythz_blog/?p=474

### How is the database structured ?

There is a global [Document] table. Each index is a custom class which has its own table. A reduce index also adds a bridge table in order to map many documents. 
Internally YesSql communicates with the database server using [Dapper](https://github.com/StackExchange/dapper-dot-net).

### Dude ! Why another document database ?

I know :/ Well actually I am a big fan of document databases and I am well aware that some like MongoDb and RavenDb are already top-notch ones, but __what if you want a free, transactional .NET document database__ ?

* MongoDb is not transactional, and some applications can't cope with it. RDBMS on the contrary are all transactional. 
* RavenDb (which I am a big fan of) is not free. Also the fact that it's using custom serialization libraries and Lucene to store the data might scare some companies which have invested a lot in SQL, trust SQL, and have in-house experts.

So YesSql might be an answer for the developers who face those restrictions. If you don't care about those then please don't spend one more minute on YesSql, it's useless for you.

### I am sold, where do I start ?

The documentation is here: https://github.com/sebastienros/yessql/wiki

You can also take a look at the [sample apps](https://github.com/sebastienros/yessql/tree/master/samples) in the source code.
