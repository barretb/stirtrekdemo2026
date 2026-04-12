# Rusty — History

## Core Context

- **Project:** An OpenTelemetry demonstration app built on .NET Aspire v13 with an ASP.NET Web API backend and Blazor Server frontend.
- **Role:** Backend Dev
- **Joined:** 2026-04-12T01:51:06.776Z

## Learnings

- **Gitignore for .NET Solutions**: Updated .gitignore to properly exclude .NET build artifacts (bin/, obj/), NuGet packages, Visual Studio IDE files (.vs/, *.user, *.csproj.user), test results, Aspire artifacts, and environment-specific configs. This prevents accidental commits of generated/temporary files and keeps the repository clean.
- **Repository State**: Build artifacts and IDE files were previously being tracked; the updated gitignore now properly handles these. Pre-existing build errors (MvcApplicationParts target) are unrelated to gitignore changes.
- **Key Paths**: StirTrekDemo solution consists of ApiService, Web (Blazor), ServiceDefaults, and AppHost (orchestrator) projects using .NET 10.
- **Session 2026-04-12**: Orchestration log created for gitignore update task completion. All decisions merged into decisions.md. Scribe duties completed.
- **Git Tracking Issue**: .gitignore patterns don't remove files already tracked. Had to explicitly remove 53 tracked bin/obj build artifacts via `git rm --cached`, then commit deletion. .gitignore now works correctly for future builds—new artifacts are ignored automatically.
- **Git Index Cleanup Follow-up**: `.gitignore` already ignored `bin/` and `obj/`, but 261 build-artifact paths were still tracked in Git after earlier cleanup attempts. Resolved by removing them from the index with `git rm --cached` while leaving files on disk, then re-verifying `git ls-files` returned no `bin/` or `obj/` entries.
- **W3C Baggage Propagation Bug**: The "Inspect Baggage" feature always returned empty results because `GetBaggageAsync` in `MissionApiClient.cs` made a bare GET request with no active Activity. Without an active Activity, .NET's HttpClient doesn't inject the `baggage` header. Fixed by modifying `GetBaggageAsync` to accept `commanderName` and `priority` parameters, start an Activity using `telemetry.ActivitySource.StartActivity("baggage.inspect")`, set baggage on the Activity before making the HTTP call. This ensures .NET auto-injects the baggage header and the API receives the baggage context.
- **Baggage Feature Complete** (2026-04-12): Coordinated with Linus on frontend updates. Baggage inspection now works end-to-end without launching a mission. Feature verified to propagate baggage via W3C header correctly.
- **OTel Metric Instruments for Fuel and Shield** (2025-01-13): The Aspire dashboard showed warp core temperature but not fuel or shield, even though GetTelemetry() returned those values. Root cause: readings were only returned in HTTP response, never recorded as OTel metrics. Implemented Option A (observable gauges in MissionTelemetryService): Added per-mission dictionaries (`_fuelLevels`, `_shieldStrengths`) with public `UpdateFuelLevel()` and `UpdateShieldStrength()` methods. Created `ObservableGauge<double>` instruments (`missions.fuel.level`, `missions.shield.strength`, unit: `%`) that emit one measurement per mission with a `mission.id` tag. Modified `MissionService.GetTelemetry()` to push readings into telemetry service after generating them. Dashboard now shows live per-mission fuel and shield gauges alongside warp core temperature.
