"&{$Branch='dev';iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.ps1'))}"
& dnvm update-self
& dnvm upgrade
& dnvm install 1.0.0-rc1-update1
& dnvm install -r coreclr -arch x64 latest
& dnvm alias default 1.0.0-rc1-update1 -r coreclr
& dnvm use default -p
& dnvm list

# YesSql.Core
& dnu restore .\src\YesSql.Core\project.json
& dnu build .\src\YesSql.Core\project.json

# YesSql.Storage.Sql
& dnu restore .\src\YesSql.Storage.Sql\project.json
& dnu build .\src\YesSql.Storage.Sql\project.json

# YesSql.Storage.LightningDB
& dnu restore .\src\YesSql.Storage.LightningDB\project.json
& dnu build .\src\YesSql.Storage.LightningDB\project.json

# YesSql.Storage.InMemory
& dnu restore .\src\YesSql.Storage.InMemory\project.json
& dnu build .\src\YesSql.Storage.InMemory\project.json

# YesSql.Storage.Cache
& dnu restore .\src\YesSql.Storage.Cache\project.json
& dnu build .\src\YesSql.Storage.Cache\project.json

# YesSql.Tests
& dnu restore .\test\YesSql.Tests\project.json
& dnu build .\test\YesSql.Tests\project.json
