# Zyntra

A desktop application for managing Roblox accounts, launching games, and automating workflows with Lua scripting.

## Features

- **Account Management** — Add, tag, filter, import/export Roblox accounts with encrypted cookie storage
- **Game Launching** — Launch games per account or mass-launch by tag, with recently played history
- **Lua Scripting** — Built-in script editor with auto-injected API (MoonSharp engine)
- **Applications** — Register and launch external apps with custom args, env vars, and working directories
- **Plugin System** — Extend Zyntra with .NET class library plugins
- **Auto-Updater** — Checks GitHub releases for new versions
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

- Added settings: default page, disable animations, auto-refresh cookies, default tag for new accounts, hide invalid accounts, default script template, clear recently played, check for updates on startup
- Removed Dashboard page — app opens directly to the configured default page
- Migrated scripting engine to Lua (MoonSharp) — removed PowerShell, Batch, Python
- Redesigned Docs tab with horizontal tabs, feature cards, and numbered steps
- Apps now launch with working directory set to the exe's folder
- Version read from assembly at runtime (date-based versioning: YY.M.patch)
- Build script auto-increments version; GitHub Actions creates releases
