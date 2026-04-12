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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
