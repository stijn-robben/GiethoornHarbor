# GiethoornHarbor

A comprehensive microservices-based harbor management system built with .NET Core and Docker. This system manages all aspects of harbor operations including ship management, dock allocation, billing, water quality monitoring, and ship services.

## 🏗️ Architecture

This system follows a microservices architecture with the following services:

### Core Services

- **Harbor Management Service** (Port 5282) - Manages ship arrivals, departures, and scheduling
- **Dock & Shipment Company Service** (Port 5283) - Handles dock allocation and rental management
- **Ship Service** (Port 5208) - Manages ship services including refueling, electricity, and container handling
- **Billing Service** (Port 5206) - Processes payments and generates invoices
- **Water Management Service** (Port 5171) - Monitors water quality and environmental conditions

### Infrastructure

- **RabbitMQ** (Ports 5672, 15672) - Message broker for inter-service communication
- **SQL Server Databases** - Separate databases for each service
  - Harbor DB (Port 1434)
  - Water DB (Port 1435)
  - Billing DB (Port 1436)
  - Dock DB (Port 1437)
  - Ship Service DB (Port 1438)

## 🚀 Getting Started

### Prerequisites

- Docker and Docker Compose
- .NET 8.0 SDK (for development)
- Visual Studio 2022 or VS Code (recommended)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/GiethoornHarbor.git
   cd GiethoornHarbor
   ```

2. **Start all services**
   ```bash
   docker-compose up -d
   ```

3. **Verify services are running**
   ```bash
   docker-compose ps
   ```

### Service Endpoints

Once running, the following endpoints will be available:

- **Harbor Management**: http://localhost:5282
- **Dock Management**: http://localhost:5283
- **Ship Service**: http://localhost:5208
- **Billing**: http://localhost:5206
- **Water Management**: http://localhost:5171
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

## 📋 Features

### Ship Management
- Track ship arrivals and departures
- Manage ship status (Planned, Arrived, Departed)
- Service scheduling and coordination

### Dock Management
- Real-time dock availability tracking
- Rental management with pricing
- Company-dock assignments
- Automated invoicing for dock rentals

### Ship Services
- **Refueling Services** - Fuel management and billing
- **Electricity Services** - Power supply for docked ships
- **Container Handling** - Automated loading/unloading with support for:
  - Normal cargo
  - Fresh goods
  - Livestock
  - Fragile items
  - Hazardous materials

### Billing System
- Automated invoice generation
- Payment processing
- Company account management
- Service-based billing integration

### Water Quality Monitoring
- Real-time water quality assessment
- Quality scoring (0-100 scale)
- Environmental compliance tracking
- Automated alerts for quality issues

## 🛠️ Development

### Project Structure

```
GiethoornHarbor/
├── HarborManagementService/     # Ship scheduling and management
├── Dock-ShipmentCompany/        # Dock allocation and rentals
├── ShipService/ShipService/     # Ship services (fuel, electricity, containers)
├── Billing/Billing/             # Payment processing and invoicing
├── WaterManagement/             # Environmental monitoring
├── docker-compose.yml           # Docker orchestration
└── GiethoornHarbor.sln         # Visual Studio solution
```

### Building from Source

1. **Restore dependencies**
   ```bash
   dotnet restore
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Run tests** (if available)
   ```bash
   dotnet test
   ```

### Database Migrations

Each service manages its own database. To apply migrations:

```bash
# Example for Harbor Management Service
cd HarborManagementService
dotnet ef database update
```

## 🔧 Configuration

### Environment Variables

Each service can be configured using environment variables:

- `ConnectionStrings__DefaultConnection` - Database connection string
- `RabbitMQ__HostName` - RabbitMQ server hostname
- `ASPNETCORE_ENVIRONMENT` - Environment (Development/Production)

### Database Configuration

The system uses SQL Server with the following default configuration:
- **Username**: sa
- **Password**: Your_password123!
- **Trust Server Certificate**: True

## 📡 Message Flow

Services communicate via RabbitMQ using event-driven architecture:

1. **Ship Arrival** → Triggers dock allocation and service scheduling
2. **Service Completion** → Generates billing events
3. **Payment Processing** → Updates company accounts
4. **Water Quality Changes** → Alerts and compliance tracking

