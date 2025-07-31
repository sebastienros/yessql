#!/bin/bash

# YesSql Multi-Database Test Runner
# Runs tests on all supported databases with failure resilience

# Default values
CONFIGURATION="Release"
FRAMEWORK="net8.0"
TEST_FILTER=""
SKIP_BUILD=false

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Help function
show_help() {
    echo "YesSql Multi-Database Test Runner"
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -c, --configuration CONFIG  Build configuration (Debug or Release). Default: Release"
    echo "  -f, --framework FRAMEWORK   Target framework (net6.0 or net8.0). Default: net8.0"
    echo "  -t, --test-filter FILTER    Optional test filter to run specific tests"
    echo "  -s, --skip-build            Skip building the solution before running tests"
    echo "  -h, --help                  Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0"
    echo "  $0 -c Debug -f net6.0"
    echo "  $0 -t \"ShouldCompareDateTimeOffsetWithDateTime\""
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -f|--framework)
            FRAMEWORK="$2"
            shift 2
            ;;
        -t|--test-filter)
            TEST_FILTER="$2"
            shift 2
            ;;
        -s|--skip-build)
            SKIP_BUILD=true
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Test configurations
declare -a TEST_CONFIGS=(
    "SQLite .NET 8.0|YesSql.Tests.SqliteTests|net8.0"
    "SQLite .NET 6.0|YesSql.Tests.SqliteTests|net6.0"
    "PostgreSQL .NET 8.0|YesSql.Tests.PostgreSqlTests|net8.0"
    "PostgreSQL .NET 6.0|YesSql.Tests.PostgreSqlTests|net6.0"
    "MySQL .NET 8.0|YesSql.Tests.MySqlTests|net8.0"
    "MySQL .NET 6.0|YesSql.Tests.MySqlTests|net6.0"
    "SQL Server 2019 .NET 8.0|YesSql.Tests.SqlServer2019Tests|net8.0"
    "SQL Server 2019 .NET 6.0|YesSql.Tests.SqlServer2019Tests|net6.0"
    "PostgreSQL Legacy Identity .NET 6.0|YesSql.Tests.PostgreSqlLegacyIdentityTests|net6.0"
    "SQLite Legacy Identity .NET 6.0|YesSql.Tests.SqliteLegacyIdentityTests|net6.0"
)

# Results tracking
declare -a RESULTS=()
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

echo -e "${CYAN}YesSql Multi-Database Test Runner${NC}"
echo -e "${CYAN}=================================${NC}"
echo -e "${GRAY}Configuration: $CONFIGURATION${NC}"
echo -e "${GRAY}Framework: $FRAMEWORK (override per test config)${NC}"
if [[ -n "$TEST_FILTER" ]]; then
    echo -e "${GRAY}Test Filter: $TEST_FILTER${NC}"
fi
echo ""

# Build the solution if not skipped
if [[ "$SKIP_BUILD" != true ]]; then
    echo -e "${YELLOW}Building solution...${NC}"
    if ! dotnet build --configuration "$CONFIGURATION"; then
        echo -e "${RED}Build failed. Exiting.${NC}"
        exit 1
    fi
    echo -e "${GREEN}Build completed successfully.${NC}"
    echo ""
fi

# Create results file
RESULTS_FILE="test-results-$(date +%Y%m%d-%H%M%S).json"
echo "[" > "$RESULTS_FILE"

# Run tests for each database configuration
for i in "${!TEST_CONFIGS[@]}"; do
    IFS='|' read -r TEST_NAME FILTER FW <<< "${TEST_CONFIGS[$i]}"
    
    echo -e "${CYAN}[$TEST_NAME]${NC} Running tests..."
    
    # Build test filter command
    FILTER_ARG="$FILTER"
    if [[ -n "$TEST_FILTER" ]]; then
        FILTER_ARG="($FILTER)&($TEST_FILTER)"
    fi
    
    # Run test command
    START_TIME=$(date +%s.%N)
    
    TEST_OUTPUT=$(dotnet test \
        --configuration "$CONFIGURATION" \
        --filter "$FILTER_ARG" \
        --no-restore \
        --no-build \
        --framework "$FW" \
        --logger "console;verbosity=minimal" 2>&1)
    
    EXIT_CODE=$?
    END_TIME=$(date +%s.%N)
    DURATION=$(echo "$END_TIME - $START_TIME" | bc)
    
    # Parse results and update counters
    if [[ $EXIT_CODE -eq 0 ]]; then
        echo -e "${CYAN}[$TEST_NAME]${NC} ${GREEN}PASSED${NC} ${GRAY}(${DURATION}s)${NC}"
        STATUS="PASSED"
        ((PASSED_TESTS++))
    else
        echo -e "${CYAN}[$TEST_NAME]${NC} ${RED}FAILED${NC} ${GRAY}(${DURATION}s)${NC}"
        STATUS="FAILED"
        
        # Extract first error line
        ERROR_LINE=$(echo "$TEST_OUTPUT" | grep -E "(Failed|Error|Exception)" | head -1 || echo "Unknown error")
        echo -e "  ${RED}Error: $ERROR_LINE${NC}"
        ((FAILED_TESTS++))
    fi
    
    # Add result to JSON file (append comma if not first)
    if [[ $i -gt 0 ]]; then
        echo "," >> "$RESULTS_FILE"
    fi
    
    cat >> "$RESULTS_FILE" << EOF
  {
    "name": "$TEST_NAME",
    "framework": "$FW",
    "status": "$STATUS",
    "exitCode": $EXIT_CODE,
    "duration": $DURATION,
    "output": $(echo "$TEST_OUTPUT" | jq -R -s .)
  }
EOF
    
    ((TOTAL_TESTS++))
    echo ""
done

# Close JSON file
echo "]" >> "$RESULTS_FILE"

# Summary
echo -e "${CYAN}Test Execution Summary${NC}"
echo -e "${CYAN}=====================${NC}"
echo -e "Total Configurations: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"
echo ""

if [[ $FAILED_TESTS -gt 0 ]]; then
    echo -e "${RED}Failed Configurations:${NC}"
    echo -e "${RED}----------------------${NC}"
    
    # Read and display failed configurations
    jq -r '.[] | select(.status == "FAILED") | "  - " + .name + " (" + .framework + ")"' "$RESULTS_FILE" | \
    while IFS= read -r line; do
        echo -e "${RED}$line${NC}"
    done
    echo ""
fi

echo -e "${GREEN}Passed Configurations:${NC}"
echo -e "${GREEN}----------------------${NC}"
jq -r '.[] | select(.status == "PASSED") | "  - " + .name + " (" + .framework + ")"' "$RESULTS_FILE" | \
while IFS= read -r line; do
    echo -e "${GREEN}$line${NC}"
done

echo ""
echo -e "${GRAY}Detailed results saved to: $RESULTS_FILE${NC}"

# Exit with appropriate code
if [[ $FAILED_TESTS -gt 0 ]]; then
    echo ""
    echo -e "${YELLOW}Some database configurations failed. Check the results above.${NC}"
    exit 1
else
    echo ""
    echo -e "${GREEN}All database configurations passed!${NC}"
    exit 0
fi