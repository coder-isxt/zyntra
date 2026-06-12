-- ============================================================
--  Fracture Script API — Lua Module
--  Auto-loaded into every Lua script run by Fracture.
--  See the Docs tab in Fracture for full documentation.
-- ============================================================

fracture = {}

-- ── Context ──────────────────────────────────────────────────

function fracture.get_version()
    return _fracture_context.Version or ""
end

function fracture.get_data_dir()
    return _fracture_context.DataDir or ""
end

-- ── Accounts ─────────────────────────────────────────────────

function fracture.get_accounts()
    return _fracture_context.Accounts or {}
end

function fracture.get_account(name)
    for _, acc in ipairs(fracture.get_accounts()) do
        if acc.Username == name or acc.DisplayName == name then
            return acc
        end
    end
    return nil
end

function fracture.get_accounts_by_tag(tag)
    local result = {}
    for _, acc in ipairs(fracture.get_accounts()) do
        if acc.Tag == tag then
            table.insert(result, acc)
        end
    end
    return result
end

function fracture.get_account_count()
    return #fracture.get_accounts()
end

-- ── Apps ─────────────────────────────────────────────────────

function fracture.get_apps()
    return _fracture_context.Apps or {}
end

function fracture.get_app(name)
    for _, app in ipairs(fracture.get_apps()) do
        if app.Name == name then
            return app
        end
    end
    return nil
end

function fracture.get_app_count()
    return #fracture.get_apps()
end

-- ── Recently Played ─────────────────────────────────────────

function fracture.get_recently_played()
    return _fracture_context.RecentGames or {}
end

function fracture.get_last_played()
    local games = fracture.get_recently_played()
    if #games > 0 then return games[1] end
    return nil
end

-- ── Notifications ────────────────────────────────────────────

function fracture.notify(title, message, type)
    type = type or "Info"
    _fracture_notify(title, message, type)
end

-- ── Clipboard ────────────────────────────────────────────────

function fracture.set_clipboard(text)
    _fracture_set_clipboard(text)
end

-- ── Game Launch ──────────────────────────────────────────────

function fracture.launch_game(account_name, place_id)
    _fracture_launch_game(account_name, place_id)
end

function fracture.launch_game_all(place_id, tag)
    local accounts
    if tag then
        accounts = fracture.get_accounts_by_tag(tag)
    else
        accounts = fracture.get_accounts()
    end
    for _, acc in ipairs(accounts) do
        fracture.launch_game(acc.Username, place_id)
    end
end

-- ── Utilities ────────────────────────────────────────────────

function fracture.log(message)
    _fracture_log(tostring(message))
end

function fracture.sleep(ms)
    _fracture_sleep(ms)
end

-- ── UI ──────────────────────────────────────────────────────

fracture.ui = {}

function fracture.ui.create_tab(name, icon)
    icon = icon or ""
    return _fracture_ui_create_tab(name, icon)
end

function fracture.ui.add_label(tab, text, opts)
    opts = opts or {}
    _fracture_ui_add_label(tab, text, opts.font_size or 14, opts.bold or false)
end

function fracture.ui.add_button(tab, text, callback)
    _fracture_ui_add_button(tab, text, callback)
end

function fracture.ui.add_text_input(tab, id, opts)
    opts = opts or {}
    _fracture_ui_add_text_input(tab, id, opts.placeholder or "")
end

function fracture.ui.add_separator(tab)
    _fracture_ui_add_separator(tab)
end

function fracture.ui.add_progress(tab, value, label)
    label = label or ""
    _fracture_ui_add_progress(tab, value, label)
end

function fracture.ui.add_checkbox(tab, id, text, checked)
    if checked == nil then checked = false end
    _fracture_ui_add_checkbox(tab, id, text, checked)
end

function fracture.ui.add_dropdown(tab, id, label, options, selected_index)
    selected_index = selected_index or 1
    _fracture_ui_add_dropdown(tab, id, label, options, selected_index)
end

function fracture.ui.get_value(tab, id)
    return _fracture_ui_get_value(tab, id)
end

return fracture
