Remove-Item ".\artifacts\packages" -Recurse -ErrorAction Ignore

dotnet pack .\src\YesSql.Core -c release -o $pwd\artifacts\packages

dotnet pack .\src\YesSql.Provider.InMemory -c release -o $pwd\artifacts\packages
dotnet pack .\src\YesSql.Provider.MySql -c release -o $pwd\artifacts\packages
dotnet pack .\src\YesSql.Provider.PostgreSql -c release -o $pwd\artifacts\packages
dotnet pack .\src\YesSql.Provider.Sqlite -c release -o $pwd\artifacts\packages
dotnet pack .\src\YesSql.Provider.SqlServer -c release -o $pwd\artifacts\packages

dotnet pack .\src\YesSql.Storage.InMemory -c release -o $pwd\artifacts\packages
dotnet pack .\src\YesSql.Storage.Cache -c release -o $pwd\artifacts\packages
dotnet pack .\src\YesSql.Storage.LightningDB -c release -o $pwd\artifacts\packages
dotnet pack .\src\YesSql.Storage.Sql -c release -o $pwd\artifacts\packages
