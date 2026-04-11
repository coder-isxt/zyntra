@echo off
setlocal enabledelayedexpansion

echo ========================================
echo   Zyntra Build ^& Release Tool
echo ========================================
echo.

:: ── Compute version (YY.M.patch) ────────────────────────
:: Year and month from current date
for /f "tokens=*" %%d in ('powershell.exe -NoProfile -Command "(Get-Date).ToString('yy')"') do set "YY=%%d"
for /f "tokens=*" %%d in ('powershell.exe -NoProfile -Command "[int](Get-Date).Month"') do set "MM=%%d"

:: Find latest tag matching this year.month prefix, auto-increment patch
set "PREFIX=%YY%.%MM%"
set "PATCH=0"
for /f "tokens=*" %%i in ('git tag -l "v%PREFIX%.*" --sort^=-v:refname 2^>nul') do (
    if "!PATCH!"=="0" (
        set "TAG=%%i"
        set "TAG=!TAG:v=!"
        for /f "tokens=3 delims=." %%p in ("!TAG!") do set /a PATCH=%%p
    )
)
set /a PATCH=%PATCH%+1
set "VERSION=%PREFIX%.%PATCH%"

echo New version: v%VERSION%
echo.

:: ── Changelog ────────────────────────────────────────────
echo Current README.md changelog:
echo ----------------------------------------
powershell.exe -NoProfile -Command "$r = Get-Content README.md -Raw; $i = $r.IndexOf('## Changelog'); if ($i -ge 0) { Write-Output $r.Substring($i) } else { Write-Output '(no changelog section found)' }"
echo ----------------------------------------
echo.
echo Edit README.md now if needed, then press any key to continue...
pause >nul

:: ── Update .csproj version ───────────────────────────────
echo.
echo Updating version to %VERSION% in Zyntra.csproj...
powershell.exe -NoProfile -Command "$f = 'Zyntra\Zyntra.csproj'; $c = [System.IO.File]::ReadAllText($f); $c = $c -replace '<Version>[^<]*</Version>', '<Version>%VERSION%</Version>'; $c = $c -replace '<AssemblyVersion>[^<]*</AssemblyVersion>', '<AssemblyVersion>%VERSION%.0</AssemblyVersion>'; $c = $c -replace '<FileVersion>[^<]*</FileVersion>', '<FileVersion>%VERSION%.0</FileVersion>'; [System.IO.File]::WriteAllText($f, $c)"

:: ── Kill running Zyntra ──────────────────────────────────
taskkill /f /im Zyntra.exe >nul 2>&1
timeout /t 2 /nobreak >nul

:: ── Clean + Build ────────────────────────────────────────
if exist build rmdir /s /q build >nul 2>&1

echo.
echo Building Zyntra v%VERSION% (Release, single-file)...
dotnet publish Zyntra\Zyntra.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=false -o build

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build FAILED!
    pause
    exit /b 1
)

echo.
echo Build successful! Output: build\Zyntra.exe
echo.

:: ── Git commit, tag, push ────────────────────────────────
echo Committing and pushing to GitHub...
git add -A
git commit -m "Release v%VERSION%"
git tag -a "v%VERSION%" -m "v%VERSION%"
git push origin main
git push origin "v%VERSION%"

echo.
echo ========================================
echo   Released v%VERSION% to GitHub!
echo   GitHub Actions will create the release.
echo ========================================
echo.