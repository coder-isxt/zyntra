# Zyntra Feature Roadmap

A comprehensive feature plan to evolve Zyntra from a Roblox-focused bootstrapper into a full-featured, open-source universal game launcher and app manager.

---

## Phase 1 — Polish & GitHub-Ready (High Priority)

These are table-stakes for a credible open-source launch.

- **Auto-updater** — Check GitHub Releases for new versions, show update prompt, download and replace exe in-place.
- **Theme system** — Let users pick accent colors (not just purple). Store in settings. Potential for full light/dark toggle.
- **Keybinds / hotkeys** — Global hotkey to show/hide Zyntra (e.g. `Ctrl+Shift+Z`). Configurable in Settings.
- **Import/export accounts** — Export encrypted account data to a file, import on another machine. Useful for backup.
- **Multi-language support (i18n)** — Resource-file based localization. English + community-contributed translations.
- **README + screenshots + logo** — GitHub-ready docs, feature list, installation instructions, contributing guide.
- **CI/CD with GitHub Actions** — Auto-build releases on tag push, attach single-file exe to GitHub Releases.

## Phase 2 — Enhanced Roblox Features

Deepen the Roblox integration — this is what will attract users initially.

- **Server browser / favorites** — Save favorite Place IDs with names. Quick-launch from a list instead of typing IDs.
- **Multi-instance launch** — Launch multiple Roblox accounts simultaneously into the same or different servers (multi-Roblox via Mutex bypass).
- **Account groups / tags** — Organize accounts with labels like "Alts", "Mains", "Trading". Filter and batch-launch.
- **Server region selector** — Show available server list for a Place ID (via Roblox Games API), let user pick a specific server/job ID.
- **Cookie health monitor** — Background check if cookies are still valid. Show a warning badge on expired accounts.
- **Roblox FPS unlocker integration** — Option to auto-launch an FPS unlocker alongside Roblox.
- **Game activity log** — Track which accounts joined which games and when. Simple local history.

## Phase 3 — Multi-Game Support

Expand beyond Roblox with a plugin-like module architecture.

- **Game module system** — Abstract `IGameModule` interface: `ValidateAccount()`, `LaunchGame()`, `GetAvatar()`. Roblox becomes the first module.
- **Minecraft module** — Support Microsoft/Mojang accounts, launch Java or Bedrock edition, manage multiple profiles.
- **Steam module** — Quick-switch between Steam accounts (via registry user swap). Launch specific Steam games.
- **Epic Games module** — Account switching and game launching via Epic's protocol URIs.
- **Custom game modules** — Let users define simple launch profiles for any game: exe path, arguments, environment variables.

## Phase 4 — Power User Features

Features that differentiate Zyntra from basic launchers.

- **Plugin system** — Load `.dll` plugins from a `plugins/` folder. Provide SDK/API for community extensions.
- **Scripting / automation** — Simple task runner: "At 8pm, launch Account X into Place Y". Cron-like scheduler.
- **Discord Rich Presence** — Show "Launching via Zyntra" or current game status in Discord.
- **Webhook notifications** — Send a Discord/HTTP webhook when an account launches, cookie expires, etc.
- **System resource monitor** — Show CPU/RAM/GPU usage in a sidebar widget. Track per-game resource usage.
- **Portable mode** — Detect if running from USB/portable folder, store all data next to exe instead of %APPDATA%.

## Phase 5 — Community & Social

Features that build a community around the tool.

- **Cloud sync (optional)** — Sync settings and favorites (NOT cookies) across devices via GitHub Gist or a simple backend.
- **Community server lists** — Curated/shared lists of popular Roblox servers that users can subscribe to.
- **Theme marketplace** — Share and download custom color themes from a community repo.
- **Plugin repository** — Browse and install community plugins from within Zyntra.

## Phase 6 — UI/UX Enhancements

Ongoing polish to make the app feel premium.

- **Dashboard / home page** — Overview: recent launches, account status, quick actions.
- **Drag-and-drop reorder** — Reorder apps and accounts via drag-and-drop in the list.
- **Search / filter bar** — Quick search across all accounts and apps.
- **Notifications panel** — In-app notification center: cookie expiry warnings, update alerts, launch confirmations.
- **Animated transitions** — Page transition animations between sidebar tabs.
- **Compact mode** — Smaller window / mini-mode for quick launching without the full UI.
- **Custom window shapes** — Rounded corners, blur effects (Mica/Acrylic on Win11).

---

## Suggested Implementation Order

| Priority | Feature | Impact |
|----------|---------|--------|
| 🔴 Now | README + CI/CD + GitHub setup | Launch readiness |
| 🔴 Now | Theme accent color picker | Visual appeal |
| 🔴 Now | Favorite Places / quick-launch list | Core UX |
| 🟠 Soon | Auto-updater | Retention |
| 🟠 Soon | Account groups/tags | Organization |
| 🟠 Soon | Multi-instance Roblox | Power users |
| 🟠 Soon | Cookie health monitor | Reliability |
| 🟡 Next | Game module system | Architecture |
| 🟡 Next | Dashboard home page | Polish |
| 🟡 Next | Global hotkeys | Convenience |
| 🔵 Later | Plugin system | Extensibility |
| 🔵 Later | Multi-game modules | Growth |
| 🔵 Later | Discord Rich Presence | Social |
