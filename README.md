# Stir Trek Demo 2026 - Space Mission Control

A .NET Aspire demonstration application showcasing OpenTelemetry distributed tracing, metrics, logs, and W3C Baggage propagation using a Star Trek-themed mission control system.

## 🚀 Project Overview

This solution demonstrates modern observability patterns in a distributed .NET application using:
- **.NET Aspire** for orchestration and service discovery
- **OpenTelemetry** for distributed tracing, metrics, and logging
- **W3C Trace Context** and **Baggage** propagation
- **Blazor** for the interactive frontend
- **Minimal APIs** for the backend service

## 📁 Solution Structure

- **StirTrekDemo.AppHost** - .NET Aspire orchestrator that runs the entire solution
- **StirTrekDemo.Web** - Blazor frontend for mission control interface
- **StirTrekDemo.ApiService** - Backend API service managing space missions
- **StirTrekDemo.ServiceDefaults** - Shared OpenTelemetry configuration

## 🎯 Features

### Mission Management
- View and manage five Star Trek vessels: Enterprise, Voyager, Deep Space Nine, Discovery, and Defiant
- Launch missions with success/failure simulation
- Real-time telemetry monitoring (fuel, shields, distance)
- Reset all missions to initial state

### Observability
- **Distributed Tracing**: Full request correlation between frontend and API
- **Metrics**: Custom mission metrics, HTTP performance, and runtime metrics
- **Structured Logging**: Correlated logs with trace context
- **W3C Baggage**: Mission commander and priority propagation across service boundaries

### API Endpoints

```
GET    /api/missions              - List all missions
GET    /api/missions/{id}         - Get mission details
POST   /api/missions/{id}/launch  - Launch a mission
GET    /api/missions/{id}/telemetry - Get mission telemetry
POST   /api/missions/reset        - Reset all missions
GET    /api/diagnostics/baggage   - View W3C Baggage data
```

## 🛠️ Getting Started

### Prerequisites
- .NET 10 SDK
- Visual Studio 2026 or later (or VS Code with C# Dev Kit)

### Running the Application

1. Clone the repository:
   ```bash
   git clone https://github.com/barretb/stirtrekdemo2026
   cd stirtrekdemo
   ```

2. Run the application using the AppHost:
   ```bash
   dotnet run --project StirTrekDemo.AppHost
   ```

3. The Aspire Dashboard will open automatically, showing:
   - Application resources and their status
   - Distributed traces
   - Metrics dashboards
   - Structured logs
   - Health checks

4. Access the Mission Control UI via the `webfrontend` endpoint shown in the dashboard

## 📊 Observability Deep Dive

### Custom Telemetry Sources

**Activity Sources (Traces)**:
- `StirTrekDemo.ApiService` - Backend operations
- `StirTrekDemo.Web` - Frontend operations

**Meters (Metrics)**:
- `StirTrekDemo.ApiService` - Mission launch counters, telemetry gauges
- `StirTrekDemo.Web` - Frontend interaction metrics

### W3C Baggage Example

The application demonstrates baggage propagation by sending mission metadata from the frontend to the backend:
- `mission.commander` - The commanding officer
- `mission.priority` - Mission priority level

This data is automatically propagated through HTTP headers and accessible in both services.

## 🧪 Demo Scenarios

1. **Successful Launch**: Launch Enterprise or Voyager missions
2. **Failed Launch**: The Defiant mission is configured to fail on first attempt
3. **Trace Correlation**: Observe how traces span from Blazor UI through HttpClient to API endpoints
4. **Baggage Propagation**: Check the `/api/diagnostics/baggage` endpoint to see propagated metadata

## 🔧 Technologies

- .NET 10
- C# 14
- ASP.NET Core Minimal APIs
- Blazor
- .NET Aspire
- OpenTelemetry
- OTLP (OpenTelemetry Protocol)

## 📝 Learn More

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [W3C Baggage](https://www.w3.org/TR/baggage/)

## 🎤 Stir Trek Conference

This demo was created for Stir Trek 2026, showcasing modern observability practices in distributed .NET applications.

## 📄 License

This project is for educational and demonstration purposes.