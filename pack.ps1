param (
   [string]$solutionPath = ".\YesSql.sln",
   [string]$destinationLocalNuget = "..\localNugets"
)

Remove-Item .\nupkgs -Recurse -ErrorAction Ignore
dotnet pack $solutionPath -c release -o $pwd\nupkgs --nologo --version-suffix "tpc-portal-ymaz1" -v q
Remove-Item $destinationLocalNuget -Recurse -ErrorAction Ignore
nuget init .\nupkgs $destinationLocalNuget

