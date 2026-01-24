# Quick Start Script for Test Result Browser
# This script helps you get started with Docker deployment

Write-Host "=== Test Result Browser - Docker Quick Start ===" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is installed
Write-Host "Checking Docker installation..." -ForegroundColor Yellow
$dockerInstalled = $null -ne (Get-Command docker -ErrorAction SilentlyContinue)

if (-not $dockerInstalled) {
    Write-Host "ERROR: Docker is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install Docker Desktop from https://www.docker.com/products/docker-desktop" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Docker is installed" -ForegroundColor Green

# Check Docker daemon is running
Write-Host "Checking Docker daemon..." -ForegroundColor Yellow
$dockerRunning = docker info 2>&1 | Select-String "Containers" -ErrorAction SilentlyContinue

if ($LASTEXITCODE -ne 0 -or $null -eq $dockerRunning) {
    Write-Host "[ERROR] Docker daemon is not running" -ForegroundColor Red
    Write-Host ""
    Write-Host "Docker Desktop must be running before starting containers." -ForegroundColor Yellow
    Write-Host "Please:" -ForegroundColor Yellow
    Write-Host "  1. Start Docker Desktop (look for it in your Start menu or taskbar)" -ForegroundColor White
    Write-Host "  2. Wait for Docker to fully start (usually takes 30-60 seconds)" -ForegroundColor White
    Write-Host "  3. Run this script again" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "[OK] Docker daemon is running" -ForegroundColor Green

# Prompt for test results path
Write-Host ""
Write-Host "Configure Test Results Path" -ForegroundColor Cyan
Write-Host "Enter the path to your test results directory:" -ForegroundColor Yellow
Write-Host "(For testing, you can use: $PWD\..\sample_data)" -ForegroundColor Gray

$testResultsPath = Read-Host "Path"

if ([string]::IsNullOrWhiteSpace($testResultsPath)) {
    $testResultsPath = "$PWD\..\sample_data"
    Write-Host "Using default: $testResultsPath" -ForegroundColor Gray
}

# Validate path exists
if (-not (Test-Path $testResultsPath)) {
    Write-Host "ERROR: Path does not exist: $testResultsPath" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Test results path validated" -ForegroundColor Green

# Set environment variable
$env:TEST_RESULTS_PATH = $testResultsPath

# Display configuration
Write-Host ""
Write-Host "Configuration:" -ForegroundColor Cyan
Write-Host "  Test Results Path: $testResultsPath" -ForegroundColor White
Write-Host "  Application Port:  http://localhost:5000" -ForegroundColor White
Write-Host "  Memory Limit:      20GB" -ForegroundColor White
Write-Host ""

# Build and start
Write-Host "Building and starting container..." -ForegroundColor Yellow
Write-Host "(This may take a few minutes on first run)" -ForegroundColor Gray
Write-Host ""

# Change to script directory to find docker-compose.yml
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

docker-compose up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=== SUCCESS ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Test Result Browser is now running!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Access the application at: http://localhost:5000" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Useful commands:" -ForegroundColor Yellow
    Write-Host "  View logs:        docker-compose logs -f testresultbrowser" -ForegroundColor White
    Write-Host "  Stop container:   docker-compose down" -ForegroundColor White
    Write-Host "  Restart:          docker-compose restart" -ForegroundColor White
    Write-Host "  Check health:     docker ps" -ForegroundColor White
    Write-Host ""
    
    # Ask if user wants to view logs
    $viewLogs = Read-Host "View logs now? (y/n)"
    if ($viewLogs -eq 'y' -or $viewLogs -eq 'Y') {
        Write-Host ""
        Write-Host "Press Ctrl+C to exit logs view" -ForegroundColor Gray
        Start-Sleep -Seconds 2
        docker-compose logs -f testresultbrowser
    }
} else {
    Write-Host ""
    Write-Host "=== ERROR ===" -ForegroundColor Red
    Write-Host ""
    Write-Host "Failed to start container. Check the error messages above." -ForegroundColor Red
    Write-Host ""
    Write-Host "Common issues and solutions:" -ForegroundColor Yellow
    Write-Host "  1. Docker daemon not running" -ForegroundColor White
    Write-Host "     → Start Docker Desktop from your Start menu or taskbar" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Port 5000 already in use" -ForegroundColor White
    Write-Host "     → Stop other services or change the port in docker-compose.yml" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. Insufficient memory" -ForegroundColor White
    Write-Host "     → Docker needs 20GB RAM available (adjust in Docker Desktop settings)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  4. Invalid test results path" -ForegroundColor White
    Write-Host "     → Verify the path exists and has proper permissions" -ForegroundColor Gray
    Write-Host ""
    Write-Host "For more help, see: docker/README.md" -ForegroundColor Cyan
    Write-Host ""
    exit 1
}
