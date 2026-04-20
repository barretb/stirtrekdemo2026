# Squad Decisions

## Active Decisions

### ADR: OTEL Demo Stack
**Status:** Accepted  
**Author:** Danny (Coordinator)  
**Date:** 2026-04-12

Application scaffolded using **Aspire Starter (ASP.NET Core/Blazor)** template on .NET 10:
- StirTrekDemo.AppHost — Aspire orchestration host with Dashboard
- StirTrekDemo.ServiceDefaults — centralized OTEL configuration
- StirTrekDemo.ApiService — ASP.NET Core Minimal API backend
- StirTrekDemo.Web — Blazor Server frontend with service discovery

All services inherit OTEL instrumentation (traces, metrics, logs) via ServiceDefaults.

**Key Constraint:** All future services must reference ServiceDefaults and call `builder.AddServiceDefaults()`.

---

### Decision: .NET Solution Gitignore
**Status:** Implemented  
**Author:** Rusty (Backend Dev)  
**Date:** 2025-01-13

Updated `.gitignore` to exclude .NET/Visual Studio artifacts:
- Build outputs: bin/, obj/, out/
- Binaries: *.dll, *.exe, *.pdb, *.so, *.dylib
- NuGet: packages/, *.nupkg, *.snupkg
- IDE: .vs/, .vscode/, *.user, *.csproj.user
- Testing: TestResults/, *.trx, coverage/
- Aspire/Dev: .aspnet/, .env, launchSettings.json, appsettings

Repository now clean; build artifacts properly ignored going forward.

---

### Decision: Space Mission Control OTEL Backend
**Status:** Implemented  
**Author:** Rusty (Backend Dev)  
**Date:** 2025-04-11

Built ApiService backend with Star Trek theme:
- **MissionTelemetryService** owns ActivitySource and Meter (single source of truth)
- **MissionService** owns in-memory mission store with inline OTEL instrumentation
- Baggage propagated via W3C header (auto-extracted by ASP.NET Core instrumentation)
- Defiant always fails first launch (demo trigger); others 10% random failure
- All 4 phases execute regardless of outcome (complete trace hierarchy)

Endpoints: `/api/missions`, `/api/missions/{id}`, `/api/missions/{id}/launch`, `/api/missions/{id}/telemetry`, `/api/diagnostics/baggage`

---

### Decision: Purge tracked .NET build outputs from index
**Status:** Completed  
**Author:** Danny (Lead)  
**Date:** 2026-04-12

The repository had `bin/` and `obj/` ignore rules, but build outputs remained in Git history because they were tracked before the ignore rules took effect.

**Resolution:**
- Kept existing `.gitignore` rules for `.NET` build outputs
- Removed 261 tracked `bin/` and `obj/` paths from the Git index with `git rm --cached`
- Preserved developers' local build artifacts on disk
- Verified cleanup with `git ls-files` — no `bin/` or `obj/` entries remain in working tree

**Result:** Repository now has a clean index boundary; developers can build locally without polluting git status. Build artifacts never tracked again.

---

### Decision: Fix Baggage Inspection Feature
**Status:** Implemented  
**Author:** Rusty (Backend Dev) & Linus (Frontend Dev)  
**Date:** 2026-04-12

## Problem

The "Inspect Baggage" feature always returned empty results ("No baggage found"), even though baggage propagation worked correctly during mission launches.

## Root Cause

`GetBaggageAsync` in `MissionApiClient.cs` made a bare GET request to `/api/diagnostics/baggage` with no active `Activity`. Without an active Activity, .NET's HttpClient does not inject the `baggage` header into outgoing HTTP requests. The API received the request with no baggage headers, so `Baggage.Current.GetBaggage()` returned an empty dictionary every time.

## Solution

**Backend (Rusty):** Modified `GetBaggageAsync` in `StirTrekDemo.Web\Services\MissionApiClient.cs` to:
1. Accept `commanderName` and `priority` as parameters (with default empty strings for backward compatibility)
2. Start an Activity using `telemetry.ActivitySource.StartActivity("baggage.inspect")`
3. Set baggage on the Activity before making the HTTP call:
   - `activity?.SetBaggage("mission.commander", commanderName)` (if non-empty)
   - `activity?.SetBaggage("mission.priority", priority)` (if non-empty)
4. Make the HTTP call (the active Activity causes .NET to auto-inject the `baggage` header)

New signature:
```csharp
public async Task<Dictionary<string, string>?> GetBaggageAsync(
    string commanderName = "",
    string priority = "",
    CancellationToken cancellationToken = default)
```

**Frontend (Linus):** Updated `InspectBaggage` method in `MissionControl.razor` to pass `_commanderName` and `_priority` to `GetBaggageAsync()`. Form field bindings were already in scope; only needed to be threaded through the API call.

## Impact

- Baggage inspection now works correctly — baggage is propagated via W3C header
- UI can now demonstrate baggage propagation without launching a mission
- Consistent with the existing `LaunchMissionAsync` pattern (which already sets baggage correctly)
- No changes required to API service — purely a client-side fix

---

### Decision: Observable Gauges for Fuel and Shield Telemetry
**Status:** Implemented  
**Author:** Rusty (Backend Dev)  
**Date:** 2026-04-12

## Problem

The Aspire dashboard displayed the `missions.warpcore.temperature` gauge but not fuel level or shield strength, even though `GetTelemetry()` returned those values in the HTTP response. The telemetry readings were generated but never recorded as OTel metric instruments.

## Root Cause

`MissionService.GetTelemetry()` generated random fuel (0–100) and shield (0–100) values per call and recorded a span with tags, but did NOT record metric instruments. The readings were returned in the API response but never fed into the Meter.

## Solution

Implemented **Observable Gauges in MissionTelemetryService** (matches warp core pattern):

### MissionTelemetryService Changes

1. Added per-mission state dictionaries:
   - `Dictionary<string, double> _fuelLevels`
   - `Dictionary<string, double> _shieldStrengths`

2. Added public update methods:
   ```csharp
   public void UpdateFuelLevel(string missionId, double value)
   public void UpdateShieldStrength(string missionId, double value)
   ```

3. Created `ObservableGauge<double>` instruments:
   - `missions.fuel.level` (unit: `%`)
   - `missions.shield.strength` (unit: `%`)
   - Each emits one measurement per mission with a `mission.id` tag

### MissionService Changes

Modified `GetTelemetry()` to push readings into telemetry service after generating them:
```csharp
_telemetry.UpdateFuelLevel(id, reading.FuelLevel);
_telemetry.UpdateShieldStrength(id, reading.ShieldStrength);
```

## Impact

- Aspire dashboard now shows live per-mission fuel and shield gauges alongside warp core temperature
- Observable gauges update whenever telemetry is polled via GET `/api/missions/{id}/telemetry`
- Consistent pattern with existing warp core temperature gauge
- Better demo visibility for OTEL metrics showcase

---

### Decision: Demo Features — Reset Missions and Force Failure

**Status:** Implemented  
**Author:** Rusty (Backend Dev)  
**Date:** 2025-01-13

## Context

For presentation demos, needed two utility features in the API service:
1. A way to reset all missions back to their initial state
2. A way to force mission launch failures on demand

## Implementation

### 1. Reset Missions Endpoint

Added `POST /api/missions/reset` endpoint that resets the demo environment:

**MissionService.cs:**
- New public method `ResetMissions()` that:
  - Resets all 5 missions (Enterprise, Voyager, DS9, Discovery, Defiant) to `MissionStatus.Preparing`
  - Restores original launch dates relative to current time:
    - Enterprise: +7 days
    - Voyager: +14 days
    - DS9: +3 days
    - Discovery: +21 days
    - Defiant: +1 day
  - Clears `_guaranteedFailureFired` HashSet so Defiant will fail on next launch (preserves demo behavior)
  - Returns int count of missions reset
  - Logs: `"Missions reset: {Count} missions returned to Preparing"`

**Program.cs:**
- Registered endpoint: `POST /api/missions/reset`
- Returns: `{ reset: count, message: "All missions reset to Preparing." }`
- Endpoint name: `"ResetMissions"`

### 2. Force Failure Query Parameter

Updated launch endpoint to support forced failures for demo scenarios:

**MissionService.cs:**
- Changed signature: `LaunchMissionAsync(string id)` → `LaunchMissionAsync(string id, bool forceFailure = false)`
- When `forceFailure = true`, sets `shouldFail = true` immediately (bypasses both random 10% logic and Defiant first-launch logic)
- Preserves existing failure phase selection and OTEL instrumentation

**Program.cs:**
- Updated `POST /api/missions/{id}/launch` to accept `forceFailure` bool query parameter (defaults to false)
- Example usage: `POST /api/missions/enterprise/launch?forceFailure=true`

## Design Decisions

- **Reset is stateless**: Doesn't track previous state, just resets to hardcoded initial values
- **Reset clears failure tracker**: Ensures Defiant demo behavior works across multiple demo runs
- **Force failure is opt-in**: Default behavior unchanged; requires explicit query param
- **No OTEL on reset**: Reset is a utility endpoint for demo control, not a business operation
- **Existing OTEL preserved**: Launch instrumentation unchanged; force failure traces identical to natural failures

## Impact

- Presenters can reset demo environment between presentations without restarting the app
- Presenters can demonstrate failure scenarios on-demand without relying on 10% random chance or Defiant first-launch trigger
- All existing mission launch behavior preserved when `forceFailure` is not specified
- Clean separation between business logic and demo utilities

---

### Decision: Presentation-Friendly UI Features

**Status:** Implemented  
**Author:** Linus (Frontend Dev)  
**Date:** 2025-01-13

## Problem

Mission Control needed four presentation-friendly features to make OTEL demos more engaging and easier to demonstrate:
1. Ability to reset all missions back to Preparing state
2. Force failure toggle for predictable demo scenarios  
3. Load generator to quickly create multiple traces/metrics
4. Easy TraceId copying to transition to Aspire Dashboard

## Solution

### Feature 1: Reset All Missions

**Backend (`MissionApiClient.cs`):**
- Added `ResetMissionsAsync()` method that calls `POST /api/missions/reset`
- Returns `(int Reset, string Message)` tuple
- Maintains OTEL instrumentation pattern with ApiCallDuration metric

**Frontend (`MissionControl.razor`):**
- Added "🔄 Reset All" button (btn-outline-danger) in MISSION ROSTER header
- Clears selected mission and launch results on reset
- Logs reset count to activity log
- Disabled while loading

### Feature 2: Force Failure Toggle

**Backend (`MissionApiClient.cs`):**
- Updated `LaunchMissionAsync` signature to accept `bool forceFailure = false`
- Appends `?forceFailure=true` query parameter when enabled
- **Bug fix:** Removed `EnsureSuccessStatusCode()` that was discarding 422 responses — now reads LaunchResult regardless of HTTP status code

**Frontend (`MissionControl.razor`):**
- Added `_forceFailure` bool field
- Added checkbox below priority dropdown with "☠️ Force Failure" label and demo hint
- Passes `_forceFailure` to LaunchMissionAsync call

### Feature 3: Load Generator

**Frontend (`MissionControl.razor`):**
- Added `_generating` bool and `_generateProgress` string fields
- Created `GenerateLoad()` method:
  - Finds all missions with `Status == MissionStatus.Preparing`
  - Launches each with commander "Load Generator", priority "High", no forced failure
  - 600ms delay between launches (spreads traces in Aspire timeline)
  - Updates `_generateProgress` with "N/M" counter
  - Logs each result to activity log
  - Reloads missions after completion
- Added "⚡ Generate Load" button (btn-outline-success) in MISSION ROSTER header
- Disabled when no Preparing missions exist or already generating
- Shows spinner + progress counter while running

### Feature 4: Copy TraceId to Clipboard

**Frontend (`MissionControl.razor`):**
- Injected `IJSRuntime` service
- Added `CopyToClipboard(string text)` async method using JS interop:
  - Calls `navigator.clipboard.writeText` via JS
  - Silently catches errors (clipboard API may not be available in all contexts)
- Added "📋 Copy" button next to TraceId display in launch result card
- TraceId remains full length (not truncated) in the card
- Note: Activity log still shows truncated TraceId (kept for compact display)

## UI Placement

- **Reset All** and **Load Generator** buttons: Added to MISSION ROSTER card header in a `btn-group` with existing Refresh button
- **Force Failure** checkbox: Placed below priority dropdown in Launch Control panel
- **Copy TraceId** button: Inline with TraceId display in launch result alert

## Impact

- Presenters can quickly reset demo state between scenarios
- Force failure enables predictable "failure trace" demonstrations
- Load generator creates multiple traces with one click for metrics visualization
- TraceId copy reduces friction when transitioning to Aspire Dashboard exploration
- Maintains all existing OTEL instrumentation (no regression)
- Star Trek aesthetic preserved

## Technical Notes

- All four features coordinate with Rusty's backend changes (reset and force-failure endpoints)
- Fixed existing bug where 422 status code from failed launches was throwing before reading response body
- All OTEL instrumentation preserved (Activity, Baggage, Metrics)
- Force failure parameter defaults to false for backward compatibility

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
