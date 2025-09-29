# ChatApp API

Chat application backend built with .NET 8 and SQL Server, containerized with Docker.

## ?? Quick Start

### Prerequisites

- Docker & Docker Compose
- .NET 8 SDK (for local development)

### Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ChatAppAPI
   ```

2. **Setup environment variables**
   ```bash
   # Copy the example file
   cp .env.example .env
   
   # Edit .env with your values
   nano .env
   ```

3. **Run the application**
   ```bash
   # Start development environment
   docker-compose up -d
   
   # View logs
   docker-compose logs -f chatapi
   ```

4. **Access the application**
   - API: http://localhost:44000
   - API (HTTPS): https://localhost:44001
   - SQL Server: localhost:15000

### Production Setup

1. **Prepare environment**
   ```bash
   # Create production environment file
   cp .env.example .env.prod
   
   # Edit with production values
   nano .env.prod
   ```

2. **Deploy to production**
   ```bash
   # Build and push to registry (optional)
   docker-compose build
   docker tag chatapi your-registry/chatapi:latest
   docker push your-registry/chatapi:latest
   
   # Run production setup
   docker-compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env.prod up -d
   ```

## ?? Project Structure

```
ChatAppAPI/
??? ChatApi/                 # Main API project
??? ChatApi.Core/           # Domain models and interfaces
??? ChatApi.Services/       # Business logic services
??? ChatApi.Infrastructure/ # Data access and external services
??? docker-compose.yml      # Base Docker configuration
??? docker-compose.override.yml # Development overrides
??? docker-compose.prod.yml # Production configuration
??? .env.example           # Environment variables template
??? .env                   # Your environment variables (git-ignored)
```

## ?? Configuration

### Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `DB_PASSWORD` | SQL Server password | `MyStrongPassword123` |
| `JWT_SECRET` | JWT signing secret | `your-jwt-secret-key` |
| `JWT_ISSUER` | JWT token issuer | `https://api.yourdomain.com` |
| `JWT_AUDIENCE` | JWT token audience | `https://yourdomain.com` |
| `DB_SERVER` | Database server name | `sqlserver` |
| `DB_PORT` | Database port | `1433` |
| `DB_NAME` | Database name | `chatAppDb` |
| `DB_USER` | Database user | `sa` |

### JWT Settings

The application supports comprehensive JWT configuration:
- Token validation (issuer, audience, lifetime, signing key)
- Access token expiration (default: 24 hours)
- Refresh token expiration (default: 30 days)

## ?? Docker Commands

### Development
```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# View logs
docker-compose logs -f [service-name]

# Rebuild services
docker-compose build
```

### Production
```bash
# Start production environment
docker-compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env.prod up -d

# Stop production environment
docker-compose -f docker-compose.yml -f docker-compose.prod.yml down
```

## ??? Database

- **Engine**: Microsoft SQL Server 2022
- **Development Port**: 15000
- **Production Port**: 1433
- **Default Database**: chatAppDb

### Connection Examples

**Development:**
```
Server=localhost,15000;Database=chatAppDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

**Production:**
```
Server=sqlserver,1433;Database=chatAppDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

## ?? Development

### Local Development (without Docker)

1. **Setup SQL Server**
   ```bash
   # Start only SQL Server
   docker-compose up sqlserver -d
   ```

2. **Update appsettings.Development.json**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost,15000;Database=chatAppDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
     }
   }
   ```

3. **Run the API**
   ```bash
   cd ChatApi
   dotnet run
   ```

### Building Docker Image

```bash
# Build the image
docker build -t chatapi -f ChatApi/Dockerfile .

# Tag for registry
docker tag chatapi your-registry/chatapi:latest

# Push to registry
docker push your-registry/chatapi:latest
```

## ?? API Endpoints

The API will be available at:
- Development: http://localhost:44000
- Production: http://localhost (port 80)

Swagger documentation available at `/swagger` endpoint.

## ?? Security Notes

1. **Never commit `.env` files** - They contain sensitive information
2. **Use strong passwords** for production databases
3. **Generate secure JWT secrets** (minimum 32 characters)
4. **Update default passwords** before deployment
5. **Use HTTPS in production** for JWT token security

## ?? Troubleshooting

### Common Issues

1. **Port conflicts**
   ```bash
   # Check if ports are in use
   netstat -an | findstr :44000
   netstat -an | findstr :15000
   ```

2. **SQL Server connection issues**
   ```bash
   # Check SQL Server health
   docker-compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourPassword -C -Q "SELECT 1"
   ```

3. **Environment variables not loaded**
   ```bash
   # Verify environment file
   docker-compose config
   ```

### Logs

```bash
# View all logs
docker-compose logs

# View specific service logs
docker-compose logs chatapi
docker-compose logs sqlserver

# Follow logs in real-time
docker-compose logs -f chatapi
```

## ?? License

This project is licensed under the MIT License.

## ?? Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

---

**Happy Coding! ??**