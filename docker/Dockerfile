# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/TestResultBrowser.Web/TestResultBrowser.Web.csproj", "TestResultBrowser.Web/"]
RUN dotnet restore "TestResultBrowser.Web/TestResultBrowser.Web.csproj"

# Copy source code and build
COPY src/TestResultBrowser.Web/ TestResultBrowser.Web/
WORKDIR "/src/TestResultBrowser.Web"
RUN dotnet build "TestResultBrowser.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TestResultBrowser.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install dependencies for file system access and networking
RUN apt-get update && apt-get install -y \
    cifs-utils \
    && rm -rf /var/lib/apt/lists/*

# Create directories for data
RUN mkdir -p /app/data /app/userdata

# Copy published app
COPY --from=publish /app/publish .

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Environment variables (can be overridden in docker-compose or runtime)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV TestResultBrowser__FileSharePath=/mnt/testresults
ENV TestResultBrowser__UserDataPath=/app/userdata

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TestResultBrowser.Web.dll"]
