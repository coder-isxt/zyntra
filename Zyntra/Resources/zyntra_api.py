"""
Zyntra Script API — Python Module
Auto-imported into every Python script run by Zyntra.
See the Docs tab in Zyntra for full documentation.
"""

import json, os, sys, atexit
from datetime import datetime

_context_path = os.environ.get("ZYNTRA_CONTEXT", "")
_context = None
_response = {"Notifications": [], "SetClipboard": None}


def _load_context():
    global _context
    if _context is None and _context_path and os.path.exists(_context_path):
        with open(_context_path, "r", encoding="utf-8") as f:
            _context = json.load(f)


def _flush_response():
    _load_context()
    if _context and _context.get("ResponseFile"):
        with open(_context["ResponseFile"], "w", encoding="utf-8") as f:
            json.dump(_response, f, indent=2)

atexit.register(_flush_response)


# ── Context ──────────────────────────────────────────────────

def get_version() -> str:
    """Returns the current Zyntra version string."""
    _load_context()
    return _context.get("Version", "") if _context else ""

def get_data_dir() -> str:
    """Returns the Zyntra AppData directory path."""
    _load_context()
    return _context.get("DataDir", "") if _context else ""


# ── Accounts ─────────────────────────────────────────────────

def get_accounts() -> list:
    """Returns all Roblox accounts as dicts."""
    _load_context()
    return _context.get("Accounts", []) if _context else []

def get_account(name: str) -> dict | None:
    """Find an account by username or display name."""
    for acc in get_accounts():
        if acc.get("Username") == name or acc.get("DisplayName") == name:
            return acc
    return None

def get_accounts_by_tag(tag: str) -> list:
    """Returns accounts filtered by tag."""
    return [a for a in get_accounts() if a.get("Tag") == tag]

def get_account_count() -> int:
    """Returns the number of accounts."""
    return len(get_accounts())


# ── Apps ─────────────────────────────────────────────────────

def get_apps() -> list:
    """Returns all registered applications as dicts."""
    _load_context()
    return _context.get("Apps", []) if _context else []

def get_app(name: str) -> dict | None:
    """Find an app by name."""
    for app in get_apps():
        if app.get("Name") == name:
            return app
    return None

def get_app_count() -> int:
    """Returns the number of apps."""
    return len(get_apps())


# ── Notifications ────────────────────────────────────────────

def send_notification(title: str, message: str, type: str = "Info"):
    """
    Sends a notification to the Zyntra notification panel.
    type: Info, Success, Warning, Error
    """
    _response["Notifications"].append({
        "Title": title,
        "Message": message,
        "Type": type,
    })


# ── Clipboard ────────────────────────────────────────────────

def set_clipboard(text: str):
    """Sets the Windows clipboard text via Zyntra."""
    _response["SetClipboard"] = text


# ── Utilities ────────────────────────────────────────────────

def log(message: str):
    """Writes a timestamped log line to stdout."""
    ts = datetime.now().strftime("%H:%M:%S")
    print(f"[{ts}] {message}")
