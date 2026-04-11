param(
    [Parameter(Mandatory)][string]$Version
)

$repo = "coder-isxt/zyntra"
$tag = "v$Version"
$exePath = "build\Zyntra.exe"
$changelogPath = "CHANGELOG.txt"

# Get token from environment or git config
$token = $env:GITHUB_TOKEN
if (-not $token) { $token = git config --get github.token 2>$null }
if (-not $token) {
    Write-Host "WARNING: No GITHUB_TOKEN found. Set GITHUB_TOKEN env var or run: git config github.token YOUR_TOKEN" -ForegroundColor Yellow
    Write-Host "Skipping GitHub release upload." -ForegroundColor Yellow
    exit 0
}

$headers = @{
    Authorization = "token $token"
    Accept        = "application/vnd.github+json"
}

# Read changelog
$body = ""
if (Test-Path $changelogPath) {
    $body = Get-Content $changelogPath -Raw
}

# Create release
Write-Host "Creating release $tag..."
$releaseData = @{
    tag_name   = $tag
    name       = $tag
    body       = $body
    draft      = $false
    prerelease = $false
} | ConvertTo-Json -Depth 10 -Compress
$jsonBytes = [System.Text.Encoding]::UTF8.GetBytes($releaseData)

try {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases" `
        -Method Post -Headers $headers -Body $jsonBytes -ContentType "application/json; charset=utf-8"
} catch {
    $err = $_.ErrorDetails.Message
    if (-not $err) { $err = $_.Exception.Message }
    Write-Host "Failed to create release: $err" -ForegroundColor Red
    exit 1
}

# Upload exe
$uploadUrl = $release.upload_url -replace '\{[^}]*\}', ''
Write-Host "Uploading Zyntra.exe..."

try {
    $bytes = [System.IO.File]::ReadAllBytes($exePath)
    Invoke-RestMethod -Uri "$($uploadUrl)?name=Zyntra.exe&label=Zyntra.exe" `
        -Method Post -Headers $headers -Body $bytes -ContentType "application/octet-stream" | Out-Null
    Write-Host "Release $tag created with Zyntra.exe attached." -ForegroundColor Green
} catch {
    Write-Host "Failed to upload exe: $_" -ForegroundColor Red
    exit 1
}
