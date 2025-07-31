#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs YesSql tests on all supported databases with failure resilience.

.DESCRIPTION
    This script runs tests for all supported database providers and continues
    execution even if individual database tests fail. It logs results for each
    database and provides a summary at the end.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Release.

.PARAMETER Framework
    Target framework (net6.0 or net8.0). Default is net8.0.

.PARAMETER TestFilter
    Optional test filter to run specific tests. If not provided, runs all tests.

.PARAMETER SkipBuild
    Skip building the solution before running tests.

.EXAMPLE
    ./test-all-databases.ps1
    
.EXAMPLE
    ./test-all-databases.ps1 -Configuration Debug -Framework net6.0
    
.EXAMPLE
    ./test-all-databases.ps1 -TestFilter "ShouldCompareDateTimeOffsetWithDateTime"
#>

param(
    [string]$Configuration = "Release",
    [string]$Framework = "net8.0",
    [string]$TestFilter = "",
    [switch]$SkipBuild
)

# Define test configurations
$testConfigs = @(
    @{ Name = "SQLite .NET 8.0"; Filter = "YesSql.Tests.SqliteTests"; Framework = "net8.0" },
    @{ Name = "SQLite .NET 6.0"; Filter = "YesSql.Tests.SqliteTests"; Framework = "net6.0" },
    @{ Name = "PostgreSQL .NET 8.0"; Filter = "YesSql.Tests.PostgreSqlTests"; Framework = "net8.0" },
    @{ Name = "PostgreSQL .NET 6.0"; Filter = "YesSql.Tests.PostgreSqlTests"; Framework = "net6.0" },
    @{ Name = "MySQL .NET 8.0"; Filter = "YesSql.Tests.MySqlTests"; Framework = "net8.0" },
    @{ Name = "MySQL .NET 6.0"; Filter = "YesSql.Tests.MySqlTests"; Framework = "net6.0" },
    @{ Name = "SQL Server 2019 .NET 8.0"; Filter = "YesSql.Tests.SqlServer2019Tests"; Framework = "net8.0" },
    @{ Name = "SQL Server 2019 .NET 6.0"; Filter = "YesSql.Tests.SqlServer2019Tests"; Framework = "net6.0" },
    @{ Name = "PostgreSQL Legacy Identity .NET 6.0"; Filter = "YesSql.Tests.PostgreSqlLegacyIdentityTests"; Framework = "net6.0" },
    @{ Name = "SQLite Legacy Identity .NET 6.0"; Filter = "YesSql.Tests.SqliteLegacyIdentityTests"; Framework = "net6.0" }
)

# Results tracking
$results = @()
$totalTests = 0
$passedTests = 0
$failedTests = 0

Write-Host "YesSql Multi-Database Test Runner" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Framework: $Framework (override per test config)" -ForegroundColor Gray
if ($TestFilter) {
    Write-Host "Test Filter: $TestFilter" -ForegroundColor Gray
}
Write-Host ""

# Build the solution if not skipped
if (-not $SkipBuild) {
    Write-Host "Building solution..." -ForegroundColor Yellow
    $buildResult = & dotnet build --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed. Exiting." -ForegroundColor Red
        exit 1
    }
    Write-Host "Build completed successfully." -ForegroundColor Green
    Write-Host ""
}

# Filter test configs by framework if specified and not using individual framework
$filteredConfigs = $testConfigs
if ($Framework -ne "net8.0" -and $Framework -ne "net6.0") {
    Write-Warning "Invalid framework specified: $Framework. Using individual test config frameworks."
} elseif ($Framework) {
    # If a specific framework is requested, filter configs or use the specified framework
    $filteredConfigs = $testConfigs | ForEach-Object {
        $config = $_.Clone()
        $config.Framework = $Framework
        $config
    }
}

# Run tests for each database configuration
foreach ($config in $filteredConfigs) {
    $testName = $config.Name
    $filter = $config.Filter
    $fw = $config.Framework
    
    Write-Host "[$testName]" -ForegroundColor Cyan -NoNewline
    Write-Host " Running tests..." -ForegroundColor White
    
    # Build test filter command
    $filterArg = $filter
    if ($TestFilter) {
        $filterArg = "($filter)&($TestFilter)"
    }
    
    # Run test command
    $testArgs = @(
        "test",
        "--configuration", $Configuration,
        "--filter", $filterArg,
        "--no-restore",
        "--no-build",
        "--framework", $fw,
        "--logger", "console;verbosity=minimal"
    )
    
    $startTime = Get-Date
    $testOutput = & dotnet @testArgs 2>&1
    $endTime = Get-Date
    $duration = $endTime - $startTime
    $exitCode = $LASTEXITCODE
    
    # Parse results
    $testResult = @{
        Name = $testName
        Framework = $fw
        ExitCode = $exitCode
        Duration = $duration
        Output = $testOutput -join "`n"
    }
    
    if ($exitCode -eq 0) {
        Write-Host "[$testName]" -ForegroundColor Cyan -NoNewline
        Write-Host " PASSED" -ForegroundColor Green -NoNewline
        Write-Host " (${duration.TotalSeconds:F1}s)" -ForegroundColor Gray
        $testResult.Status = "PASSED"
        $passedTests++
    } else {
        Write-Host "[$testName]" -ForegroundColor Cyan -NoNewline
        Write-Host " FAILED" -ForegroundColor Red -NoNewline
        Write-Host " (${duration.TotalSeconds:F1}s)" -ForegroundColor Gray
        $testResult.Status = "FAILED"
        
        # Extract error details
        $errorLines = $testOutput | Where-Object { $_ -match "Failed|Error|Exception" } | Select-Object -First 5
        if ($errorLines) {
            $testResult.ErrorSummary = $errorLines -join "; "
            Write-Host "  Error: $($errorLines[0])" -ForegroundColor Red
        }
        $failedTests++
    }
    
    $results += $testResult
    $totalTests++
    Write-Host ""
}

# Summary
Write-Host "Test Execution Summary" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host "Total Configurations: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
Write-Host ""

if ($failedTests -gt 0) {
    Write-Host "Failed Configurations:" -ForegroundColor Red
    Write-Host "----------------------" -ForegroundColor Red
    $results | Where-Object { $_.Status -eq "FAILED" } | ForEach-Object {
        Write-Host "  - $($_.Name) ($($_.Framework))" -ForegroundColor Red
        if ($_.ErrorSummary) {
            Write-Host "    $($_.ErrorSummary)" -ForegroundColor Gray
        }
    }
    Write-Host ""
}

Write-Host "Passed Configurations:" -ForegroundColor Green
Write-Host "----------------------" -ForegroundColor Green
$results | Where-Object { $_.Status -eq "PASSED" } | ForEach-Object {
    Write-Host "  - $($_.Name) ($($_.Framework))" -ForegroundColor Green
}

# Export detailed results to JSON file for CI integration
$resultsFile = "test-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$results | ConvertTo-Json -Depth 3 | Out-File $resultsFile
Write-Host ""
Write-Host "Detailed results saved to: $resultsFile" -ForegroundColor Gray

# Exit with appropriate code
if ($failedTests -gt 0) {
    Write-Host ""
    Write-Host "Some database configurations failed. Check the results above." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host ""
    Write-Host "All database configurations passed!" -ForegroundColor Green
    exit 0
}