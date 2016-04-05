Remove-Item ".\artifacts\packages" -Recurse -ErrorAction Ignore

dotnet pack .\src\YesSql.Core --configuration release --output .\artifacts\packages\YesSql.Core
dotnet pack .\src\YesSql.Storage.InMemory --configuration release --output .\artifacts\packages\YesSql.Storage.InMemory
dotnet pack .\src\YesSql.Storage.Cache --configuration release --output .\artifacts\packages\YesSql.Storage.Cache
dotnet pack .\src\YesSql.Storage.LightningDB --configuration release --output .\artifacts\packages\YesSql.Storage.LightningDB
dotnet pack .\src\YesSql.Storage.Sql --configuration release --output .\artifacts\packages\YesSql.Storage.Sql
