# Zyntra

A desktop application for managing Roblox accounts, launching games, and automating workflows with Lua scripting.

## Features

- **Account Management** — Add, tag, filter, import/export Roblox accounts with encrypted cookie storage
- **Game Launching** — Launch games per account or mass-launch by tag, with recently played history
- **Lua Scripting** — Built-in script editor with auto-injected API (MoonSharp engine)
- **Applications** — Register and launch external apps with custom args, env vars, and working directories
- **Plugin System** — Extend Zyntra with .NET class library plugins
- **Auto-Updater** — Checks GitHub releases for new versions
- **Dashboard** — Quick stats and actions at a glance
- **Notifications** — In-app notification panel with bell icon badge
- **Theming** — 9 accent color presets, dark UI throughout
- **System Tray** — Minimize to tray with quick-launch accounts

## Scripting

Zyntra uses Lua as its scripting language. The `zyntra` API is auto-injected into every script — no setup needed.

```lua
-- Launch a game with a specific account
zyntra.launch_game("MyAccount", 4483381587)
zyntra.notify("Launched", "Joining game!", "Success")
```

See the in-app **Docs** tab for the full API reference.

## Building

```
dotnet publish Zyntra\Zyntra.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o build
```

Or use `build.bat` to build, tag, and push a release automatically.

## Changelog

- Removed Dashboard page — app opens directly to Roblox Accounts
- Migrated scripting engine to Lua (MoonSharp) — removed PowerShell, Batch, Python support
- Redesigned Docs tab with horizontal tabs, feature cards, and numbered steps
- Apps now launch with working directory set to the exe's folder
- Version is read from assembly at runtime (date-based versioning: YY.M.patch)
- Build script auto-increments version and pushes to GitHub; Actions creates the release
- Removed Recently Played section from accounts page (available in launch prompt only)
- Added Recently Played games with game name resolution via Roblox API
- Added animated page transitions (fade + slide)
- Added drag-and-drop reorder for apps list
- Added Dashboard home page with stats cards and quick actions
- Added Plugin system (install, enable/disable, remove DLL plugins)
- Added Scripting tab with built-in editor
- Added search/filter bar on Applications page
- Added Notifications panel with bell icon badge
- Added account groups/tags with filtering
- Added cookie health monitor (validate cookies, status dots)
- Added custom game modules (launch args, env vars, working directory)
- Added auto-updater (GitHub releases)
- Added theme system with 9 accent color presets
- Added import/export accounts (encrypted .zyntra files)
- Added Settings page (launch on startup, minimize to tray, accent picker)
- Added system tray with quick-launch accounts
- Added app settings dialog
- Added splash screen
- Added CI/CD with GitHub Actions
- Fixed TextBox cursor/text offset in all input fields
- Fixed version display syncs with assembly version
- Fixed Roblox tab layout (filter bar no longer overlaps account list)
- Removed Steam accounts manager
