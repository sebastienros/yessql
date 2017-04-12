Remove-Item ".\artifacts\packages" -Recurse -ErrorAction Ignore

dotnet pack .\src\YesSql.Core -c release -o $pwd\artifacts\packages\YesSql.Core

dotnet pack .\src\YesSql.Provider.InMemory -c release -o $pwd\artifacts\packages\YesSql.Provider.InMemory
dotnet pack .\src\YesSql.Provider.MySql -c release -o $pwd\artifacts\packages\YesSql.Provider.MySql
dotnet pack .\src\YesSql.Provider.PostgreSql -c release -o $pwd\artifacts\packages\YesSql.Provider.PostgreSql
dotnet pack .\src\YesSql.Provider.Sqlite -c release -o $pwd\artifacts\packages\YesSql.Provider.Sqlite
dotnet pack .\src\YesSql.Provider.SqlServer -c release -o $pwd\artifacts\packages\YesSql.Provider.SqlServer

dotnet pack .\src\YesSql.Storage.InMemory -c release -o $pwd\artifacts\packages\YesSql.Storage.InMemory
dotnet pack .\src\YesSql.Storage.Cache -c release -o $pwd\artifacts\packages\YesSql.Storage.Cache
dotnet pack .\src\YesSql.Storage.LightningDB -c release -o $pwd\artifacts\packages\YesSql.Storage.LightningDB
dotnet pack .\src\YesSql.Storage.Sql -c release -o $pwd\artifacts\packages\YesSql.Storage.Sql
