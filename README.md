YesSql
=============

A .NET document database interface for relational databases, because in SQL we (still) trust !

[![Build](https://github.com/sebastienros/yessql/actions/workflows/build.yml/badge.svg)](https://github.com/sebastienros/yessql/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/YesSql.svg)](https://www.nuget.org/packages/YesSql)

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

### How is the database structured ?

There is a global [Document] table. Each index is a custom class which has its own table. A reduce index also adds a bridge table in order to map many documents. 
Internally YesSql communicates with the database server using [Dapper](https://github.com/StackExchange/dapper-dot-net).

### Dude ! Why another document database ?

I know :/ Well actually I am a big fan of document databases and I am well aware that some like MongoDb and RavenDb are already top-notch ones, but __what if you want a free, transactional .NET document database__ ?

* MongoDb is not transactional, and some applications can't cope with it. RDBMS on the contrary are all transactional. 
* RavenDb (which I am a big fan of) is not free (for most usages). 
* Some companies which have invested a lot in SQL, only trust SQL, and have in-house experts.

So YesSql might be an answer for the developers who face those restrictions. If you don't care about those then please don't spend one more minute on YesSql, it's useless for you.

### I am sold, where do I start ?

The documentation is here: https://github.com/sebastienros/yessql/wiki

You can also take a look at the [sample apps](https://github.com/sebastienros/yessql/tree/master/samples) in the source code.
