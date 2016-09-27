Remove-Item ".\artifacts\packages" -Recurse -ErrorAction Ignore

dotnet pack .\src\YesSql.Core -c release -o .\artifacts\packages\YesSql.Core
dotnet pack .\src\YesSql.Storage.InMemory -c release -o .\artifacts\packages\YesSql.Storage.InMemory
dotnet pack .\src\YesSql.Storage.Cache -c release -o .\artifacts\packages\YesSql.Storage.Cache
dotnet pack .\src\YesSql.Storage.LightningDB -c release -o .\artifacts\packages\YesSql.Storage.LightningDB
dotnet pack .\src\YesSql.Storage.Sql -c release -o .\artifacts\packages\YesSql.Storage.Sql
