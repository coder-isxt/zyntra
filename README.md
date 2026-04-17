# Zyntra

A Windows desktop app for managing multiple Roblox accounts, launching games, and automating workflows with Lua scripting. Built with WPF and .NET 8.

---

## Features

### Account Management
- Add accounts via `.ROBLOSECURITY` cookie paste or built-in browser login
- Tag, filter, and search accounts
- Cookie health checks (single or bulk)
- Import / export accounts as JSON
- Per-account notes
- Right-click context menu: launch, tag, refresh, copy username/ID, remove

### Game Launching
- Launch any account into a specific game by Place ID
- **Favorite Games** — star frequently played games for one-click launch
- Recently played history in the launch prompt
- Staggered multi-launch support

### Lua Scripting
- Built-in code editor (VS Code-style: One Dark theme, Cascadia Code font, line numbers)
- Full `zyntra` API auto-injected — no setup needed
- **Script Scheduler** — run scripts on a timer (e.g. every 60 minutes), enable/disable per script
- **Script UI** — create custom sidebar tabs with labels, buttons, inputs, checkboxes, dropdowns, and progress bars from Lua
- Duplicate and manage scripts via right-click context menu

### Applications
- Register and launch external apps with custom arguments, environment variables, and working directories
- Drag-and-drop reorder

### UI / UX
- **List / Grid view toggle** — switch between list and grid layout for Accounts and Apps
- **Toast Notifications** — slide-in popups (bottom-right) for events, auto-dismiss after 4 seconds
- **Sidebar Badges** — item count badges on nav items (toggle in Settings)
- **Notification Panel** — bell icon with unread count, mark-all-read, clear
- 9 accent color presets, fully dark themed
- System tray support — minimize to tray with quick-launch from tray menu
- Auto-updater checks GitHub releases on startup

---

## Getting Started

### Download

Grab the latest `Zyntra.exe` from the [Releases](https://github.com/coder-isxt/zyntra/releases) page. It's a single self-contained `.exe` — no install required.

### First Run

1. Launch `Zyntra.exe`
2. Go to **Roblox Accounts** and add an account:
   - **Paste cookie** — copy your `.ROBLOSECURITY` cookie from your browser and paste it
   - **Browser Login** — click "Browser Login" to sign in directly
3. Click **Launch** on any account to open Roblox, optionally entering a Place ID or picking a favorite/recent game
4. Explore **Scripts**, **Apps**, and **Docs** from the sidebar

### Settings

Open **Settings** from the sidebar to configure:
- Default startup page
- Accent color
- Sidebar badges (on/off)
- Animation toggle
- Auto-refresh cookies on startup
- Default tag for new accounts
- Default script template
- Update checks

All settings are saved to `%AppData%\Zyntra\settings.json`.

---

## Scripting

Zyntra uses **Lua** (via [MoonSharp](https://www.moonsharp.org/)) as its scripting engine. The `zyntra` API table is auto-injected into every script.

```lua
-- Log all accounts
for _, acc in ipairs(zyntra.get_accounts()) do
    zyntra.log(acc.DisplayName .. " — @" .. acc.Username)
end

-- Launch a game
zyntra.launch_game("MyAccount", 4483381587)
zyntra.notify("Done", "Game launched!", "Success")
```

### Scheduler

Enable the scheduler on any script to run it automatically on an interval:
1. Select a script in the editor
2. Toggle **Scheduler** on
3. Set the interval in minutes
4. The script runs in the background — results appear in notifications

See the in-app **Docs** tab for the full API reference.

---

## Building from Source

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (Windows)
- Git

### Build

```bash
git clone https://github.com/coder-isxt/zyntra.git
cd zyntra
dotnet publish Zyntra/Zyntra.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o build
```

Output: `build/Zyntra.exe`

### Release (maintainers)

Run `build.bat` to:
1. Auto-compute the next version (`YY.M.patch`)
2. Build a single-file Release exe
3. Commit, tag, and push to GitHub
4. GitHub Actions automatically creates a release with `Zyntra.exe` attached

---

## Project Structure

```
Zyntra/
├── Models/          # Data models (RobloxAccount, ScriptEntry, FavoriteGame, etc.)
├── ViewModels/      # MVVM ViewModels
├── Views/           # WPF UserControls and Windows
├── Services/        # Business logic (accounts, scripts, settings, scheduler, UI)
├── Converters/      # WPF value converters
├── Themes/          # DarkTheme.xaml — all styles and brushes
└── Resources/       # Embedded Lua API
```

---

## Changelog

- **Roblox Version Manager** — auto-detect, download, and install the latest Roblox player before launching; select specific versions or browse custom folders in Settings
- **Script UI API** — create custom sidebar tabs from Lua with buttons, inputs, checkboxes, dropdowns, progress bars; button callbacks; state persistence
- **List / Grid view** — toggle between list and card grid layout for Accounts and Apps; click grid cards to launch
- Removed Plugins system (replaced by Script UI)
- **Favorite Games** — save frequently played games; one-click launch from prompt; star button on recent games
- **Script Scheduler** — run scripts on a timer with configurable interval; enable/disable per script; background execution
- **Account Notes** — free-text notes per account, shown as preview on account cards
- **Toast Notifications** — slide-in toasts (bottom-right) for events; auto-dismiss after 4 seconds
- **Sidebar Badges** — item count badges on nav items; toggle in Settings > Appearance
- **Context Menus** — right-click on accounts (launch, tag, refresh, copy, remove) and scripts (run, duplicate, delete)
- Redesigned script editor with VS Code-style aesthetics
- Custom dark scrollbar with hover/drag states
- Migrated scripting engine to Lua (MoonSharp)
- Redesigned Docs tab with horizontal tabs and feature cards
- Date-based versioning (YY.M.patch); GitHub Actions auto-releases
