# StirTrekDemo Service Defaults

This shared project configures OpenTelemetry for all services in the StirTrekDemo solution.

## What's Configured

### Traces
- ASP.NET Core request tracing
- HttpClient outgoing call tracing
- Custom activity sources: `StirTrekDemo.ApiService`, `StirTrekDemo.Web`
- W3C TraceContext + Baggage propagation

### Metrics
- ASP.NET Core request metrics (duration, active requests, etc.)
- HttpClient metrics
- .NET Runtime metrics (GC, thread pool, etc.)
- Custom meters: `StirTrekDemo.ApiService`, `StirTrekDemo.Web`

### Logs
- Structured log export via OTLP
- Includes formatted message body
- Includes scopes (for correlation)
- Trace context injection (logs link to traces)

### Baggage
- W3C Baggage propagation is automatic via HttpClient
- Set baggage with `Activity.Current?.SetBaggage(key, value)`
- Read baggage with `Activity.Current?.GetBaggageItem(key)` or `Baggage.Current`

## Export
All signals are exported via OTLP to the Aspire Dashboard (automatically configured when running via AppHost).
