-- ============================================================
--  Zyntra Script API — Lua Module
--  Auto-loaded into every Lua script run by Zyntra.
--  See the Docs tab in Zyntra for full documentation.
-- ============================================================

zyntra = {}

-- ── Context ──────────────────────────────────────────────────

function zyntra.get_version()
    return _zyntra_context.Version or ""
end

function zyntra.get_data_dir()
    return _zyntra_context.DataDir or ""
end

-- ── Accounts ─────────────────────────────────────────────────

function zyntra.get_accounts()
    return _zyntra_context.Accounts or {}
end

function zyntra.get_account(name)
    for _, acc in ipairs(zyntra.get_accounts()) do
        if acc.Username == name or acc.DisplayName == name then
            return acc
        end
    end
    return nil
end

function zyntra.get_accounts_by_tag(tag)
    local result = {}
    for _, acc in ipairs(zyntra.get_accounts()) do
        if acc.Tag == tag then
            table.insert(result, acc)
        end
    end
    return result
end

function zyntra.get_account_count()
    return #zyntra.get_accounts()
end

-- ── Apps ─────────────────────────────────────────────────────

function zyntra.get_apps()
    return _zyntra_context.Apps or {}
end

function zyntra.get_app(name)
    for _, app in ipairs(zyntra.get_apps()) do
        if app.Name == name then
            return app
        end
    end
    return nil
end

function zyntra.get_app_count()
    return #zyntra.get_apps()
end

-- ── Recently Played ─────────────────────────────────────────

function zyntra.get_recently_played()
    return _zyntra_context.RecentGames or {}
end

function zyntra.get_last_played()
    local games = zyntra.get_recently_played()
    if #games > 0 then return games[1] end
    return nil
end

-- ── Notifications ────────────────────────────────────────────

function zyntra.notify(title, message, type)
    type = type or "Info"
    _zyntra_notify(title, message, type)
end

-- ── Clipboard ────────────────────────────────────────────────

function zyntra.set_clipboard(text)
    _zyntra_set_clipboard(text)
end

-- ── Game Launch ──────────────────────────────────────────────

function zyntra.launch_game(account_name, place_id)
    _zyntra_launch_game(account_name, place_id)
end

function zyntra.launch_game_all(place_id, tag)
    local accounts
    if tag then
        accounts = zyntra.get_accounts_by_tag(tag)
    else
        accounts = zyntra.get_accounts()
    end
    for _, acc in ipairs(accounts) do
        zyntra.launch_game(acc.Username, place_id)
    end
end

-- ── Utilities ────────────────────────────────────────────────

function zyntra.log(message)
    _zyntra_log(tostring(message))
end

function zyntra.sleep(ms)
    _zyntra_sleep(ms)
end

return zyntra
