# Docker & MinIO Setup Guide

## ✅ Docker Installation Status

- **Docker:** v29.4.2 ✅
- **Docker Compose:** v5.1.3 ✅
- **Docker Daemon:** Running ✅

## ✅ MinIO Installation Status

**MinIO Container is now running!**

- **Container ID:** flashfortune-backend-minio-1
- **Status:** Up and running ✅
- **Image:** minio/minio:latest
- **Ports:** 9000-9001 (both API and Console)

---

## 📍 MinIO Access Details

### API Access
- **URL:** `http://localhost:9000`
- **Access Key:** `minioadmin`
- **Secret Key:** `minioadmin`

### Console (WebUI) Access
- **URL:** `http://localhost:9001`
- **Access Key:** `minioadmin`
- **Secret Key:** `minioadmin`

### Internal Docker Network (for services in docker-compose)
- **API URL:** `http://minio:9000`
- **Endpoint:** `http://minio:9001`

---

## 📋 Configuration Files Created

### 1. `.env` File
Contains all Docker environment variables:
- PostgreSQL credentials
- MinIO credentials
- JWT secret
- Redis configuration
- Email (optional)

### 2. `appsettings.Development.json`
Local development configuration for the .NET API:
- Connects to localhost services
- MinIO (http://localhost:9000)
- PostgreSQL (localhost:5432)
- Debug logging enabled
- CORS configured for localhost:4200

---

## 🚀 Next Steps

### 1. Start PostgreSQL (Still Needed)
Install PostgreSQL 16 locally or via Docker:
```bash
# Option A: Using docker-compose (add postgres service)
docker-compose up -d postgres

# Option B: Install locally from postgresql.org
```

### 2. Start Redis (Still Needed)
```bash
# Option A: Using docker-compose (add redis service)
docker-compose up -d redis

# Option B: Install locally from redis.io
```

### 3. Start All Services with Docker Compose
Once PostgreSQL and Redis are configured:
```bash
docker-compose up -d
```

### 4. Verify MinIO Bucket
Access MinIO console at `http://localhost:9001` and create the `flashfortune` bucket:
- Login with: `minioadmin` / `minioadmin`
- Create bucket: `flashfortune`
- Enable Object Lock (for audit trail immutability)

### 5. Test the Backend
```bash
dotnet run --project src/FlashFortune.API/FlashFortune.API.csproj
```

---

## 🛑 Important Security Notes

⚠️ **Default MinIO Credentials** (from logs):
```
WARN: Detected default credentials 'minioadmin:minioadmin', 
we recommend that you change these values with 'MINIO_ROOT_USER' 
and 'MINIO_ROOT_PASSWORD' environment variables
```

**For Production:**
1. Change MinIO credentials in `.env`
2. Update `appsettings.json` with new values
3. Use strong JWT secret (already configured)
4. Enable SSL/TLS for all services
5. Use environment-specific configuration

---

## 📊 Service Status Commands

```bash
# View all running containers
docker ps

# View only FlashFortune containers
docker ps --filter "name=flashfortune"

# View MinIO logs
docker logs flashfortune-backend-minio-1

# Stop MinIO
docker stop flashfortune-backend-minio-1

# Stop all services
docker-compose down

# Restart MinIO
docker restart flashfortune-backend-minio-1
```

---

## 🔗 Service Connectivity

From your .NET application:
- **MinIO API:** `http://localhost:9000`
- **MinIO Console:** `http://localhost:9001`
- **Access Key:** `minioadmin`
- **Secret Key:** `minioadmin`

From Docker containers:
- **MinIO API:** `http://minio:9000`
- **MinIO Console:** `http://minio:9001`

---

## 📝 Troubleshooting

### MinIO Container Won't Start
```bash
# Check logs for errors
docker logs flashfortune-backend-minio-1

# Rebuild the image
docker-compose down
docker-compose up -d minio --build
```

### Connection Refused on localhost:9000
```bash
# Verify container is running
docker ps | grep minio

# Verify port mapping
docker port flashfortune-backend-minio-1

# Test connectivity
curl http://localhost:9000/minio/health/live
```

### Volume Permission Issues
```bash
# Check volume status
docker volume ls | grep flashfortune

# Inspect volume
docker volume inspect flashfortune-backend_minio_data
```

---

## 🎯 What's Still Missing

- ❌ **PostgreSQL** - Download and install, or add to docker-compose
- ❌ **Redis** - Download and install, or add to docker-compose
- ✅ **MinIO** - Now installed and running
- ✅ **Docker** - Installed

After installing PostgreSQL and Redis, you'll be ready to develop!
