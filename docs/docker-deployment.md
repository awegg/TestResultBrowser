# Docker Deployment Guide

This guide explains how to deploy the Test Result Browser using Docker and Docker Compose.

## Prerequisites

- Docker Engine 20.10+ or Docker Desktop
- Docker Compose 2.0+
- Access to the shared file system containing JUnit test results
- 12-20GB RAM available for the container

## Quick Start

### 1. Build and Run with Docker Compose

```bash
# Clone the repository
cd TestResultBrowser2.0

# Set the test results path (adjust to your environment)
export TEST_RESULTS_PATH=/path/to/shared/testresults

# Build and start the container
docker-compose up -d

# View logs
cd docker
docker-compose logs -f testresultbrowser

# Access the application
# Open browser to http://localhost:5000
```

### 2. Stop and Remove

```bash
# Stop the container
cd docker
docker-compose down

# Stop and remove volumes (WARNING: deletes user data)
cd docker
docker-compose down -v
```

## Configuration

### Environment Variables

Only deployment-specific configuration should be set via environment variables in `docker-compose.yml`:

```yaml
environment:
  # File system paths (deployment-specific)
  - TestResultBrowser__FileSharePath=/mnt/testresults
  - TestResultBrowser__UserDataPath=/app/userdata
```

**User-Configurable Settings** (polling interval, flaky test thresholds, Polarion URL, memory limits) are now managed through the **Settings** page in the application UI (accessible via the Settings link in the navigation menu). These settings are saved to the userdata database and persist across container restarts.

### Volume Mounts

#### Test Results (Read-Only)

Mount your network share or local directory containing test results:

```yaml
volumes:
  - /mnt/networkshare/testresults:/mnt/testresults:ro
```

For Windows network shares (CIFS/SMB):

```yaml
volumes:
  - type: volume
    source: testresults
    target: /mnt/testresults
    read_only: true
    volume:
      driver: local
      driver_opts:
        type: cifs
        o: "username=user,password=pass,uid=1000,gid=1000"
        device: "//server/share/testresults"
```

#### User Data (Persistent)

User-generated data (baselines, comments, saved filters) is stored in a Docker volume:

```yaml
volumes:
  - testresultbrowser-userdata:/app/userdata
```

## Docker Build Options

### Build Image Manually

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

### Multi-Stage Build Details

The Dockerfile uses a multi-stage build:

1. **Build stage**: Uses SDK image to restore, build, and publish the app
2. **Runtime stage**: Uses smaller ASP.NET runtime image for deployment

Benefits:
- Smaller final image (runtime only, no SDK)
- Faster deployments
- Better security (fewer attack vectors)

## Memory Management

### Container Memory Limits

The application requires 12-20GB RAM to cache test results in memory:

```yaml
mem_limit: 20g          # Hard limit
mem_reservation: 12g    # Soft reservation
```

### Monitor Memory Usage

```bash
# View container stats
docker stats testresultbrowser

# View detailed memory info
docker inspect testresultbrowser | grep -i memory
```

### Memory Issues

If the container is killed due to OOM (Out of Memory):

1. Increase `mem_limit` in docker-compose.yml
2. Reduce the number of builds loaded (adjust retention policy)
3. Add swap space to the host
4. Use a machine with more RAM

## Health Checks

The container includes a built-in health check:

```bash
# Check container health status
docker inspect --format='{{.State.Health.Status}}' testresultbrowser

# View health check logs
docker inspect --format='{{range .State.Health.Log}}{{.Output}}{{end}}' testresultbrowser
```

Health check endpoint: `http://localhost:8080/health`

## Networking

### Port Mapping

Default ports:
- **5000**: HTTP (mapped from container port 8080)
- **5001**: HTTPS (if configured, mapped from container port 8081)

Change ports in docker-compose.yml:

```yaml
ports:
  - "8080:8080"  # Custom HTTP port
```

### Reverse Proxy Setup

For production, use a reverse proxy (nginx, Apache, Traefik):

#### Nginx Example

```nginx
server {
    listen 80;
    server_name testbrowser.example.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

#### Traefik Example

```yaml
labels:
  - "traefik.enable=true"
  - "traefik.http.routers.testbrowser.rule=Host(`testbrowser.example.com`)"
  - "traefik.http.routers.testbrowser.entrypoints=websecure"
  - "traefik.http.services.testbrowser.loadbalancer.server.port=8080"
```

## Troubleshooting

### Container Won't Start

```bash
# Check logs
docker-compose logs testresultbrowser

# Common issues:
# 1. Invalid TEST_RESULTS_PATH
# 2. Insufficient memory
# 3. Port already in use
```

### File Access Issues

```bash
# Check volume mounts
docker inspect testresultbrowser | grep -A 10 Mounts

# Test file access inside container
docker exec -it testresultbrowser ls -la /mnt/testresults
```

### Performance Issues

```bash
# Check resource usage
docker stats testresultbrowser

# Increase memory limits if needed
# Edit docker-compose.yml and restart
docker-compose down && docker-compose up -d
```

### Network Share Issues (Windows/CIFS)

If mounting Windows network shares fails:

1. Install cifs-utils in the container (already included in Dockerfile)
2. Use credentials in volume options
3. Test connectivity: `docker exec -it testresultbrowser ping server`

## Production Deployment

### Best Practices

1. **Use a specific version tag** instead of `latest`:
   ```yaml
   image: testresultbrowser:1.0.0
   ```

2. **Set restart policy**:
   ```yaml
   restart: unless-stopped
   ```

3. **Configure logging**:
   ```yaml
   logging:
     driver: "json-file"
     options:
       max-size: "10m"
       max-file: "3"
   ```

4. **Enable HTTPS** with proper certificates

5. **Use secrets** for sensitive data:
   ```yaml
   secrets:
     - polarion_credentials
   ```

6. **Monitor with external tools** (Prometheus, Grafana)

### Backup and Restore

#### Backup User Data

```bash
# Create backup of user data volume
docker run --rm -v testresultbrowser-userdata:/data -v $(pwd):/backup \
  alpine tar czf /backup/userdata-backup.tar.gz -C /data .
```

#### Restore User Data

```bash
# Restore from backup
docker run --rm -v testresultbrowser-userdata:/data -v $(pwd):/backup \
  alpine tar xzf /backup/userdata-backup.tar.gz -C /data
```

## Kubernetes Deployment

For Kubernetes deployment, see [kubernetes-deployment.md](kubernetes-deployment.md) (coming soon).

## Support

For issues specific to Docker deployment:
1. Check logs: `docker-compose logs`
2. Verify configuration: `docker-compose config`
3. Test health endpoint: `curl http://localhost:5000/health`
4. Review this guide's troubleshooting section
