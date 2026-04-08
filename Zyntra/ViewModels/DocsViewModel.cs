namespace Zyntra.ViewModels;

public class DocsViewModel : BaseViewModel
{
    public string CurrentDoc { get; set; } = "overview";

    public static string OverviewDoc => @"
ZYNTRA SCRIPTING API
═══════════════════════════════════════════════════

Zyntra automatically injects a scripting API into every script you run.
The API gives your scripts access to Zyntra's data and lets them send
notifications, set the clipboard, and more.

SUPPORTED LANGUAGES
───────────────────
  • PowerShell  — API module auto-imported as ZyntraAPI
  • Python      — API module auto-imported as 'zyntra'
  • Batch       — Context available via %ZYNTRA_CONTEXT% env var

HOW IT WORKS
───────────────────
1. When you run a script, Zyntra exports a context JSON file containing
   your accounts, apps, and settings data.
2. The API module is injected into your script automatically.
3. After execution, Zyntra reads a response file for any notifications
   or clipboard actions your script requested.
";

    public static string PowerShellDoc => @"
POWERSHELL API REFERENCE
═══════════════════════════════════════════════════

The ZyntraAPI module is auto-imported. All functions are available
immediately in your script.

CONTEXT
───────────────────
  Get-ZyntraVersion          Returns the Zyntra version string.
  Get-ZyntraDataDir          Returns the Zyntra AppData path.

ACCOUNTS
───────────────────
  Get-ZyntraAccounts         Returns all Roblox accounts.
  Get-ZyntraAccount -Name X  Find account by username or display name.
  Get-ZyntraAccountsByTag -Tag X   Filter accounts by tag.
  Get-ZyntraAccountCount     Returns the number of accounts.

  Account properties: UserId, Username, DisplayName, Tag, CookieValid

APPS
───────────────────
  Get-ZyntraApps             Returns all registered apps.
  Get-ZyntraApp -Name X      Find an app by name.
  Get-ZyntraAppCount         Returns the number of apps.

  App properties: Id, Name, ExePath, Description, IsGameModule

NOTIFICATIONS
───────────────────
  Send-ZyntraNotification -Title X -Message Y [-Type Z]
    Sends a notification to Zyntra's panel.
    Type: Info (default), Success, Warning, Error

CLIPBOARD
───────────────────
  Set-ZyntraClipboard -Text X
    Sets the Windows clipboard after script completes.

UTILITIES
───────────────────
  Write-ZyntraLog -Message X
    Writes a timestamped log line to output.

═══════════════════════════════════════════════════

EXAMPLE: List all accounts with their tags
───────────────────────────────────────────
  $accounts = Get-ZyntraAccounts
  foreach ($acc in $accounts) {
      Write-ZyntraLog ""$($acc.DisplayName) [$($acc.Tag)]""
  }
  Send-ZyntraNotification -Title 'Done' `
      -Message ""Listed $($accounts.Count) accounts"" `
      -Type Success

EXAMPLE: Copy account names to clipboard
───────────────────────────────────────────
  $names = (Get-ZyntraAccounts).DisplayName -join ', '
  Set-ZyntraClipboard -Text $names
  Write-ZyntraLog ""Copied $((Get-ZyntraAccounts).Count) names""
";

    public static string PythonDoc => @"
PYTHON API REFERENCE
═══════════════════════════════════════════════════

The zyntra_api module is auto-imported as 'zyntra'. All functions
are available via zyntra.function_name().

CONTEXT
───────────────────
  zyntra.get_version()       Returns the Zyntra version string.
  zyntra.get_data_dir()      Returns the Zyntra AppData path.

ACCOUNTS
───────────────────
  zyntra.get_accounts()      Returns all accounts as list of dicts.
  zyntra.get_account(name)   Find account by username/display name.
  zyntra.get_accounts_by_tag(tag)  Filter accounts by tag.
  zyntra.get_account_count() Returns the number of accounts.

  Dict keys: UserId, Username, DisplayName, Tag, CookieValid

APPS
───────────────────
  zyntra.get_apps()          Returns all apps as list of dicts.
  zyntra.get_app(name)       Find an app by name.
  zyntra.get_app_count()     Returns the number of apps.

  Dict keys: Id, Name, ExePath, Description, IsGameModule

NOTIFICATIONS
───────────────────
  zyntra.send_notification(title, message, type='Info')
    Sends a notification to Zyntra's panel.
    type: 'Info', 'Success', 'Warning', 'Error'

CLIPBOARD
───────────────────
  zyntra.set_clipboard(text)
    Sets the Windows clipboard after script completes.

UTILITIES
───────────────────
  zyntra.log(message)
    Writes a timestamped log line to output.

═══════════════════════════════════════════════════

EXAMPLE: List all accounts
───────────────────────────────────────────
  for acc in zyntra.get_accounts():
      zyntra.log(f""{acc['DisplayName']} [{acc.get('Tag', 'none')}]"")

  zyntra.send_notification(
      'Done',
      f'Listed {zyntra.get_account_count()} accounts',
      'Success'
  )

EXAMPLE: Export app names to clipboard
───────────────────────────────────────────
  names = ', '.join(a['Name'] for a in zyntra.get_apps())
  zyntra.set_clipboard(names)
  zyntra.log(f'Copied {zyntra.get_app_count()} app names')
";

    public static string BatchDoc => @"
BATCH SCRIPT NOTES
═══════════════════════════════════════════════════

Batch scripts have limited API support. The context JSON path is
available via the ZYNTRA_CONTEXT environment variable.

You can read the JSON file using PowerShell from within Batch:

  for /f ""delims="" %%v in ('powershell -NoProfile -Command ^
      ""(Get-Content '%ZYNTRA_CONTEXT%' | ConvertFrom-Json).Version""') do (
      echo Zyntra version: %%v
  )

For full API access, consider using PowerShell or Python instead.
";
}
