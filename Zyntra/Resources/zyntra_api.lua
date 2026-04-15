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

-- ── UI ──────────────────────────────────────────────────────

zyntra.ui = {}

function zyntra.ui.create_tab(name, icon)
    icon = icon or ""
    return _zyntra_ui_create_tab(name, icon)
end

function zyntra.ui.add_label(tab, text, opts)
    opts = opts or {}
    _zyntra_ui_add_label(tab, text, opts.font_size or 14, opts.bold or false)
end

function zyntra.ui.add_button(tab, text, callback)
    _zyntra_ui_add_button(tab, text, callback)
end

function zyntra.ui.add_text_input(tab, id, opts)
    opts = opts or {}
    _zyntra_ui_add_text_input(tab, id, opts.placeholder or "")
end

function zyntra.ui.add_separator(tab)
    _zyntra_ui_add_separator(tab)
end

function zyntra.ui.add_progress(tab, value, label)
    label = label or ""
    _zyntra_ui_add_progress(tab, value, label)
end

function zyntra.ui.add_checkbox(tab, id, text, checked)
    if checked == nil then checked = false end
    _zyntra_ui_add_checkbox(tab, id, text, checked)
end

function zyntra.ui.add_dropdown(tab, id, label, options, selected_index)
    selected_index = selected_index or 1
    _zyntra_ui_add_dropdown(tab, id, label, options, selected_index)
end

function zyntra.ui.get_value(tab, id)
    return _zyntra_ui_get_value(tab, id)
end

return zyntra
