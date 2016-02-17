"&{$Branch='dev';iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.ps1'))}"
dnvm update-self
dnvm upgrade
dnvm install 1.0.0-rc1-update1
dnvm install -r coreclr -arch x64 latest
dnvm alias default 1.0.0-rc1-update1 -r coreclr
dnvm use default -p
dnvm list

# YesSql.Core
dnu restore .\src\YesSql.Core\project.json
dnu build .\src\YesSql.Core\project.json

# YesSql.Storage.Sql
restore .\src\YesSql.Storage.Sql\project.json
build .\src\YesSql.Storage.Sql\project.json

# YesSql.Storage.Prevalence
restore .\src\YesSql.Storage.Prevalence\project.json
build .\src\YesSql.Storage.Prevalence\project.json

# YesSql.Storage.LightningDB
restore .\src\YesSql.Storage.LightningDB\project.json
build .\src\YesSql.Storage.LightningDB\project.json

# YesSql.Storage.InMemory
restore .\src\YesSql.Storage.InMemory\project.json
build .\src\YesSql.Storage.InMemory\project.json

# YesSql.Storage.FileSystem
restore .\src\YesSql.Storage.FileSystem\project.json
build .\src\YesSql.Storage.FileSystem\project.json

# YesSql.Storage.Cache
restore .\src\YesSql.Storage.Cache\project.json
build .\src\YesSql.Storage.Cache\project.json

# YesSql.Tests
restore .\src\YesSql.Tests\project.json
build .\src\YesSql.Tests\project.json

 $env:DBCONNECTIONSTRING = "Server=(local)\\SQL2014;Database=master;User ID=sa;Password=Password12!"

 cd .\src\YesSql.Tests
 dnx test