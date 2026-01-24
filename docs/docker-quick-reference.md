# Docker Quick Reference

Quick reference for common Docker commands when working with Test Result Browser.

## Starting and Stopping

**Note**: All commands assume you're in the `docker/` directory. If you're in the project root, add `cd docker` first or use `-f docker/docker-compose.yml`.

```bash
# Navigate to docker directory
cd docker

# Start container (builds if needed)
docker-compose up -d

# Start and view logs
docker-compose up

# Stop container (keeps data)
docker-compose down

# Stop and remove all data (WARNING: deletes user data volume)
docker-compose down -v

# Restart container
docker-compose restart

# Rebuild and restart (after code changes)
docker-compose up -d --build
```

## Viewing Logs

```bash
# View all logs
docker-compose logs

# Follow logs (live tail)
docker-compose logs -f

# View last 100 lines
docker-compose logs --tail=100

# View logs for specific service
docker-compose logs testresultbrowser

# View logs with timestamps
docker-compose logs -t
```

## Inspecting Container

```bash
# Check container status
docker ps

# Check container health
docker ps --format "table {{.Names}}\t{{.Status}}"

# View detailed container info
docker inspect testresultbrowser

# View resource usage
docker stats testresultbrowser

# Check memory limits
docker inspect testresultbrowser | grep -i memory
```

## Accessing Container

```bash
# Execute command in running container
docker exec -it testresultbrowser bash

# View test results directory
docker exec testresultbrowser ls -la /mnt/testresults

# Check application logs inside container
docker exec testresultbrowser cat /app/logs/app.log

# Test network connectivity
docker exec testresultbrowser ping google.com

# View environment variables
docker exec testresultbrowser env | grep TestResultBrowser
```

## Health Checks

```bash
# Check health endpoint
curl http://localhost:5000/health

# View health check status
docker inspect --format='{{.State.Health.Status}}' testresultbrowser

# View health check logs
docker inspect --format='{{range .State.Health.Log}}{{.Output}}{{end}}' testresultbrowser
```

## Volume Management

```bash
# List volumes
docker volume ls

# Inspect user data volume
docker volume inspect testresultbrowser_testresultbrowser-userdata

# Backup user data volume
docker run --rm \
  -v testresultbrowser_testresultbrowser-userdata:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/userdata-backup.tar.gz -C /data .

# Restore user data volume
docker run --rm \
  -v testresultbrowser_testresultbrowser-userdata:/data \
  -v $(pwd):/backup \
  alpine tar xzf /backup/userdata-backup.tar.gz -C /data

# Remove unused volumes
docker volume prune
```

## Building Images

```bash
# Build image (from project root)
docker build -t testresultbrowser:latest -f docker/Dockerfile .

# Build with no cache (clean build) (from project root)
docker build --no-cache -t testresultbrowser:latest -f docker/Dockerfile .

# Build with specific tag (from project root)
docker build -t testresultbrowser:1.0.0 -f docker/Dockerfile .

# View image details
docker image inspect testresultbrowser:latest

# List images
docker images testresultbrowser

# Remove old images
docker image prune
```

## Configuration Changes

```bash
# Update environment variable
# Edit docker-compose.yml, then:
docker-compose down
docker-compose up -d

# View current configuration
docker-compose config

# Validate docker-compose.yml syntax
docker-compose config --quiet

# Override environment variable temporarily
docker-compose run -e TestResultBrowser__PollingIntervalMinutes=5 testresultbrowser
```

## Troubleshooting

```bash
# Container won't start
docker-compose logs testresultbrowser
docker events --filter 'container=testresultbrowser'

# High memory usage
docker stats testresultbrowser
docker exec testresultbrowser ps aux --sort=-%mem | head

# Network issues
docker network ls
docker network inspect testresultbrowser2.0_default

# Port conflicts
netstat -ano | findstr :5000
docker ps -a | grep testresultbrowser

# File permission issues
docker exec testresultbrowser ls -la /mnt/testresults
docker exec testresultbrowser whoami

# Check Docker daemon
docker info
docker version
```

## Cleanup

```bash
# Remove stopped containers
docker container prune

# Remove unused images
docker image prune

# Remove unused volumes
docker volume prune

# Remove unused networks
docker network prune

# Remove everything (WARNING: nuclear option)
docker system prune -a --volumes
```

## Performance Monitoring

```bash
# Real-time stats
docker stats testresultbrowser

# Export stats to file
docker stats --no-stream testresultbrowser > stats.txt

# Monitor CPU usage
docker exec testresultbrowser top

# Monitor disk I/O
docker exec testresultbrowser iostat

# Check application metrics (if exposed)
curl http://localhost:5000/metrics
```

## Common Workflows

### Daily Deployment Check

```bash
# Check status
docker ps | grep testresultbrowser

# Check health
curl http://localhost:5000/health

# Check memory
docker stats --no-stream testresultbrowser

# View recent logs
docker-compose logs --tail=50
```

### After Code Update

```bash
# Pull latest code
git pull

# Rebuild and restart
docker-compose build
docker-compose up -d

# Verify
docker-compose logs -f
```

### Troubleshoot Startup Failure

```bash
# Check logs
docker-compose logs testresultbrowser

# Check container status
docker ps -a | grep testresultbrowser

# Try running interactively
docker-compose run testresultbrowser

# Check configuration
docker-compose config

# Verify file paths
docker exec testresultbrowser ls -la /mnt/testresults
```

### Performance Investigation

```bash
# Check resource usage
docker stats testresultbrowser

# Check process list
docker exec testresultbrowser ps aux

# Check network connections
docker exec testresultbrowser netstat -tlnp

# Check disk usage
docker exec testresultbrowser df -h
```

## Docker Compose Commands

```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# View running services
docker-compose ps

# Scale service (if supported)
docker-compose up -d --scale testresultbrowser=3

# View logs
docker-compose logs -f [service]

# Execute command in service
docker-compose exec testresultbrowser bash

# Pull latest images
docker-compose pull

# Rebuild images
docker-compose build

# Validate and view config
docker-compose config
```

## Environment-Specific Commands

### Development

```bash
# Use sample data (from docker directory)
cd docker
export TEST_RESULTS_PATH=../sample_data
docker-compose up -d

# Enable debug logging
docker-compose run -e ASPNETCORE_ENVIRONMENT=Development testresultbrowser
```

### Production

```bash (from docker directory)
cd docker
# Use production data
export TEST_RESULTS_PATH=/mnt/production/testresults
docker-compose up -d

# View production logs
docker-compose logs -f --tail=100
```

## Integration with Reverse Proxy

### Nginx

```bash
# Check nginx config
docker exec nginx nginx -t

# Reload nginx
docker exec nginx nginx -s reload

# View nginx logs
docker logs nginx
```

### Traefik

```bash
# View Traefik dashboard
# Access http://localhost:8080 (if configured)

# Check routing rules
docker logs traefik | grep testbrowser
```

## Useful One-Liners

```bash
# Get container IP address
docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' testresultbrowser

# Get mapped port
docker port testresultbrowser 8080

# Check if container is running
docker ps --filter "name=testresultbrowser" --filter "status=running" --quiet

# Kill all containers
docker kill $(docker ps -q)

# Remove all containers
docker rm $(docker ps -a -q)

# Get container uptime
docker inspect --format='{{.State.StartedAt}}' testresultbrowser

# Watch logs with grep filter
docker-compose logs -f | grep ERROR
```

## Cheat Sheet

| Task | Cocd docker && docker-compose up -d` |
| Stop | `cd docker && docker-compose down` |
| Logs | `cd docker && docker-compose logs -f` |
| Restart | `cd docker && docker-compose restart` |
| Rebuild | `cd docker && ker-compose logs -f` |
| Restart | `docker-compose restart` |
| Rebuild | `docker-compose up -d --build` |
| Shell | `docker exec -it testresultbrowser bash` |
| Health | `curl http://localhost:5000/health` |
| Stats | `docker stats testresultbrowser` |
| Backup | `docker volume inspect [volume]` |

## More Information

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Reference](https://docs.docker.com/compose/compose-file/)
- [Test Result Browser Docker Guide](docker-deployment.md)
