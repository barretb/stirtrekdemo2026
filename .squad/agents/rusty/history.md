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
