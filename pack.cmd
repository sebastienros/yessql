del .\artifacts\packages /s /f /q
CALL dotnet pack .\src\YesSql.Core --configuration release --out .\artifacts\packages\YesSql.Core
CALL dotnet pack .\src\YesSql.Storage.InMemory --configuration release --out .\artifacts\packages\YesSql.Storage.InMemory
CALL dotnet pack .\src\YesSql.Storage.Cache --configuration release --out .\artifacts\packages\YesSql.Storage.Cache
CALL dotnet pack .\src\YesSql.Storage.LightningDB --configuration release --out .\artifacts\packages\YesSql.Storage.LightningDB
CALL dotnet pack .\src\YesSql.Storage.Sql --configuration release --out .\artifacts\packages\YesSql.Storage.Sql
