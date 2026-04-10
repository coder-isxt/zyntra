# ============================================================
#  Zyntra Script API — PowerShell Module
#  Auto-imported into every PowerShell script run by Zyntra.
#  See the Docs tab in Zyntra for full documentation.
# ============================================================

$script:_ContextPath = $env:ZYNTRA_CONTEXT
$script:_Context     = $null
$script:_Response    = @{ Notifications = @(); SetClipboard = $null; LaunchGame = @() }

function _LoadContext {
    if ($null -eq $script:_Context -and $script:_ContextPath -and (Test-Path $script:_ContextPath)) {
        $script:_Context = Get-Content $script:_ContextPath -Raw | ConvertFrom-Json
    }
}

# ── Context ──────────────────────────────────────────────────

function Get-ZyntraVersion {
    <# .SYNOPSIS Returns the current Zyntra version string. #>
    _LoadContext
    return $script:_Context.Version
}

function Get-ZyntraDataDir {
    <# .SYNOPSIS Returns the Zyntra AppData directory path. #>
    _LoadContext
    return $script:_Context.DataDir
}

# ── Accounts ─────────────────────────────────────────────────

function Get-ZyntraAccounts {
    <# .SYNOPSIS Returns all Roblox accounts as objects. #>
    _LoadContext
    return $script:_Context.Accounts
}

function Get-ZyntraAccount {
    <#
    .SYNOPSIS Find an account by username or display name.
    .PARAMETER Name  Username or display name to search for.
    #>
    param([string]$Name)
    _LoadContext
    return $script:_Context.Accounts | Where-Object {
        $_.Username -eq $Name -or $_.DisplayName -eq $Name
    } | Select-Object -First 1
}

function Get-ZyntraAccountsByTag {
    <#
    .SYNOPSIS Returns accounts filtered by tag.
    .PARAMETER Tag  The tag to filter by.
    #>
    param([string]$Tag)
    _LoadContext
    return $script:_Context.Accounts | Where-Object { $_.Tag -eq $Tag }
}

# ── Apps ─────────────────────────────────────────────────────

function Get-ZyntraApps {
    <# .SYNOPSIS Returns all registered applications. #>
    _LoadContext
    return $script:_Context.Apps
}

function Get-ZyntraApp {
    <#
    .SYNOPSIS Find an app by name.
    .PARAMETER Name  Application name to search for.
    #>
    param([string]$Name)
    _LoadContext
    return $script:_Context.Apps | Where-Object { $_.Name -eq $Name } | Select-Object -First 1
}

# ── Notifications ────────────────────────────────────────────

function Send-ZyntraNotification {
    <#
    .SYNOPSIS Sends a notification to the Zyntra notification panel.
    .PARAMETER Title   Notification title.
    .PARAMETER Message Notification body text.
    .PARAMETER Type    One of: Info, Success, Warning, Error. Default: Info.
    #>
    param(
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Message,
        [string]$Type = "Info"
    )
    $script:_Response.Notifications += @{
        Title   = $Title
        Message = $Message
        Type    = $Type
    }
}

# ── Clipboard ────────────────────────────────────────────────

function Set-ZyntraClipboard {
    <#
    .SYNOPSIS Sets the Windows clipboard text via Zyntra.
    .PARAMETER Text  The text to copy.
    #>
    param([Parameter(Mandatory)][string]$Text)
    $script:_Response.SetClipboard = $Text
}

# ── Utilities ────────────────────────────────────────────────

function Write-ZyntraLog {
    <#
    .SYNOPSIS Writes a timestamped log line to stdout.
    .PARAMETER Message  The log message.
    #>
    param([string]$Message)
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message"
}

function Get-ZyntraAccountCount {
    <# .SYNOPSIS Returns the number of accounts. #>
    _LoadContext
    return $script:_Context.Accounts.Count
}

function Get-ZyntraAppCount {
    <# .SYNOPSIS Returns the number of apps. #>
    _LoadContext
    return $script:_Context.Apps.Count
}

# ── Game Launch ─────────────────────────────────────────────

function Launch-ZyntraGame {
    <#
    .SYNOPSIS Launches a Roblox game with a specific account after script completes.
    .PARAMETER AccountName  Username or display name of the account.
    .PARAMETER PlaceId      The Roblox Place ID to join.
    #>
    param(
        [Parameter(Mandatory)][string]$AccountName,
        [Parameter(Mandatory)][long]$PlaceId
    )
    $script:_Response.LaunchGame += @{
        AccountName = $AccountName
        PlaceId     = $PlaceId
    }
}

function Launch-ZyntraGameAll {
    <#
    .SYNOPSIS Launches a game for all accounts, optionally filtered by tag.
    .PARAMETER PlaceId  The Roblox Place ID to join.
    .PARAMETER Tag      If set, only launch for accounts with this tag.
    #>
    param(
        [Parameter(Mandatory)][long]$PlaceId,
        [string]$Tag
    )
    _LoadContext
    $accounts = if ($Tag) {
        $script:_Context.Accounts | Where-Object { $_.Tag -eq $Tag }
    } else {
        $script:_Context.Accounts
    }
    foreach ($acc in $accounts) {
        Launch-ZyntraGame -AccountName $acc.Username -PlaceId $PlaceId
    }
}

# ── Recently Played ─────────────────────────────────────────

function Get-ZyntraRecentGames {
    <# .SYNOPSIS Returns all recently played games. #>
    _LoadContext
    return $script:_Context.RecentGames
}

function Get-ZyntraLastPlayed {
    <# .SYNOPSIS Returns the most recently played game. #>
    _LoadContext
    return $script:_Context.RecentGames | Select-Object -First 1
}

# ── Flush response on module unload ──────────────────────────

$MyInvocation.MyCommand.ScriptBlock.Module.OnRemove = {
    _LoadContext
    if ($script:_Context -and $script:_Context.ResponseFile) {
        $json = $script:_Response | ConvertTo-Json -Depth 5
        [System.IO.File]::WriteAllText($script:_Context.ResponseFile, $json)
    }
}

Export-ModuleMember -Function @(
    'Get-ZyntraVersion',
    'Get-ZyntraDataDir',
    'Get-ZyntraAccounts',
    'Get-ZyntraAccount',
    'Get-ZyntraAccountsByTag',
    'Get-ZyntraApps',
    'Get-ZyntraApp',
    'Send-ZyntraNotification',
    'Set-ZyntraClipboard',
    'Write-ZyntraLog',
    'Get-ZyntraAccountCount',
    'Get-ZyntraAppCount'
)
