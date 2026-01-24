# Test Result Browser

A web-based JUnit test results browser that provides specialized Morning Triage and Release Triage workflows for analyzing test results across multi-dimensional configuration matrices (Version × OS/DB × NamedConfig).

## Features

- **Morning Triage**: Daily view of newly failing tests grouped by domain/feature with configuration indicators
- **Release Triage**: Diff-based analysis comparing two release builds with side-by-side configuration matrices
- **Configuration History**: Track configuration changes across builds with timeline visualization
- **Performance**: <2 second filtering across 10,000+ tests and 50+ configurations
- **In-Memory Caching**: Fast access to 30M+ test results with 10-20GB RAM allocation
- **Automatic Import**: Background polling of shared file system (15-minute intervals)

## Quick Start with Docker

### Prerequisites

- Docker Engine 20.10+ or Docker Desktop
- Docker Compose 2.0+
- Access to shared file system with JUnit test results
- 12-20GB RAM available

### Run with Docker Compose

```bash
# Clone the repository
git clone <repository-url>
cd TestResultBrowser2.0

# Set the path to your test results
export TEST_RESULTS_PATH=/path/to/testresults

# Build and start the container
docker-compose up -d

# View logs
cd docker
docker-compose logs -f testresultbrowser

# Access the application
# Open browser to http://localhost:5000
```

### Build Docker Image Manually

```bash
# Build the image
docker build -t testresultbrowser:latest -f docker/Dockerfile .

# Run the container
docker run -d \
  --name testresultbrowser \
  -p 5000:8080 \
  -v /path/to/testresults:/mnt/testresults:ro \
  -v testresultbrowser-userdata:/app/userdata \
  -e TestResultBrowser__FileSharePath=/mnt/testresults \
  --memory=20g \
  --memory-reservation=12g \
  testresultbrowser:latest
```

For detailed Docker deployment instructions, see [docs/docker-deployment.md](docs/docker-deployment.md).

## Quick Start without Docker (Windows)

### Prerequisites

- .NET 8.0 SDK or Runtime
- Windows Server or Windows 10/11
- Access to shared file system (UNC path or mapped drive)

### Run from Source

```bash
# Clone the repository
git clone <repository-url>
cd TestResultBrowser2.0

# Update appsettings.json with your file share path
# Edit src/TestResultBrowser.Web/appsettings.json
# Set TestResultBrowser:FileSharePath to your test results directory

# Run the application
cd src/TestResultBrowser.Web
dotnet run

# Access the application
# Open browser to https://localhost:5001
```

## Configuration

Configuration is managed through `appsettings.json` or environment variables (recommended for Docker).

### Environment Variables

```bash
# File system paths
TestResultBrowser__FileSharePath=/mnt/testresults
TestResultBrowser__UserDataPath=/app/userdata

# Polling interval (minutes)
TestResultBrowser__PollingIntervalMinutes=15

# Flaky test detection
TestResultBrowser__FlakyTestThresholds__RollingWindowSize=20
TestResultBrowser__FlakyTestThresholds__TriggerPercentage=30
TestResultBrowser__FlakyTestThresholds__ClearAfterConsecutivePasses=10

# Polarion integration (optional)
TestResultBrowser__PolarionBaseUrl=https://polarion.example.com

# Memory limits
TestResultBrowser__MaxMemoryGB=16
```

## Architecture

- **Framework**: ASP.NET Core 8.0 Blazor Server
- **UI Library**: MudBlazor
- **Storage**: In-memory cache (ConcurrentDictionary) + LiteDB for user data
- **Background Processing**: IHostedService for file system polling
- **Platform**: Cross-platform (Docker Linux/Windows containers, Windows Server)

## Project Structure

```
src/
└── TestResultBrowser.Web/          # Blazor Server web application
    ├── Pages/                      # Blazor pages (MorningTriage, ReleaseTriage)
    ├── Components/                 # Reusable components (FilterPanel, TestHierarchy)
    ├── Services/                   # Business logic (TestDataService, TriageService)
    ├── Models/                     # Domain models (TestResult, Configuration, Build)
    └── Parsers/                    # XML parsing and metadata extraction

sample_data/                        # Sample test result XML files for development
docs/                              # Documentation (docker-deployment.md)
specs/                             # Feature specifications and implementation plans
```

## Development

### Build

```bash
dotnet build TestResultBrowser2.0.sln
```

### Test

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/TestResultBrowser.Tests.Unit/
```

### Sample Data

Use the provided sample data for development and testing:

```bash
# Validate sample data structure
cd sample_data
pwsh validate-sample-data.ps1
```

## Documentation

- [Docker Deployment Guide](docs/docker-deployment.md)
- [Feature Specification](specs/001-junit-results-browser/spec.md)
- [Implementation Plan](specs/001-junit-results-browser/plan.md)
- [Task Breakdown](specs/001-junit-results-browser/tasks.md)

## License

See [LICENSE](LICENSE) file for details.
