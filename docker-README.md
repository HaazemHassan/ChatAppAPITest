# ?? Docker Guide for ChatApp API

## Quick Start

### 1. Clone and Setup
```bash
git clone <your-repo>
cd ChatAppAPI
```

### 2. Environment Configuration
```bash
cp .env.example .env
# Edit .env file with your configurations
```

### 3. Run with Docker Compose
```bash
# Build and start all services
docker-compose up --build

# Run in background
docker-compose up -d --build

# Stop services
docker-compose down

# Stop and remove volumes (clean start)
docker-compose down -v
```

## ?? Docker Files Structure

### Dockerfile
- **Multi-stage build** for optimal image size
- **Non-root user** for security
- **Clean Architecture** support

### docker-compose.yml
- Service definitions
- Network configuration
- Dependencies management

### docker-compose.override.yml
- Development-specific configurations
- Environment variables
- Port mappings
- Volume mounts

## ?? Security Best Practices Implemented

1. **Non-root container user**
2. **Environment variables for secrets**
3. **Separate networks for services**
4. **Health checks for dependencies**
5. **Trusted certificates configuration**

## ?? Networking

- **chatapi-network**: Bridge network for service communication
- **Internal communication**: Services communicate by service name
- **External access**: 
  - API: `http://localhost:44000` (HTTP) / `https://localhost:44001` (HTTPS)
  - SQL Server: `localhost:15000`

## ?? Monitoring & Debugging

### Check Service Status
```bash
docker-compose ps
```

### View Logs
```bash
# All services
docker-compose logs

# Specific service
docker-compose logs chatapi
docker-compose logs sqlserver
```

### Execute Commands in Container
```bash
# Enter API container
docker-compose exec chatapi bash

# Run SQL commands
docker-compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourPassword
```

## ?? Troubleshooting

### Common Issues

1. **SQL Server not starting**
   ```bash
   docker-compose logs sqlserver
   # Check password complexity requirements
   ```

2. **API can't connect to database**
   ```bash
   # Check network connectivity
   docker-compose exec chatapi ping sqlserver
   ```

3. **Port conflicts**
   ```bash
   # Check if ports are in use
   netstat -an | findstr :44000
   ```

## ?? Production Considerations

### For Production Deployment:
1. Use **production Dockerfile** with optimizations
2. Set up **reverse proxy** (nginx/traefik)
3. Configure **SSL certificates** properly
4. Use **secrets management** instead of .env files
5. Set up **logging aggregation**
6. Configure **backup strategy** for volumes

### Docker Compose Production Override:
```yaml
# docker-compose.prod.yml
version: '3.8'
services:
  chatapi:
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
  sqlserver:
    restart: unless-stopped
    volumes:
      - /opt/docker/sqlserver:/var/opt/mssql
```

Run with: `docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d`