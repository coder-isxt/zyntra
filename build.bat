@echo off
setlocal enabledelayedexpansion

echo ========================================
echo   Zyntra Build ^& Release Tool
echo ========================================
echo.

:: Get latest version from git tags, default to 1.0.0
set "LATEST=1.0.0"
for /f "tokens=*" %%i in ('git describe --tags --abbrev^=0 2^>nul') do set "LATEST=%%i"
set "LATEST=!LATEST:v=!"
set "LATEST=!LATEST:V=!"

:: Auto-increment patch version
for /f "tokens=1,2,3 delims=." %%a in ("!LATEST!") do (
    set /a PATCH=%%c+1
    set "VERSION=%%a.%%b.!PATCH!"
)

echo Latest version: v%LATEST%
echo New version:    v%VERSION%
echo.

:: Show changelog
echo Current CHANGELOG.txt:
echo ----------------------------------------
type CHANGELOG.txt
echo ----------------------------------------
echo.
echo Edit CHANGELOG.txt now if needed, then press any key to continue...
pause >nul

:: Update version in UpdateService.cs using powershell.exe full path
echo.
echo Updating version to %VERSION% in source...
powershell.exe -NoProfile -Command "$f = 'Zyntra\Services\UpdateService.cs'; $c = [System.IO.File]::ReadAllText($f); $c = $c -replace 'CurrentVersion = \"[^\"]*\"', 'CurrentVersion = \"%VERSION%\"'; [System.IO.File]::WriteAllText($f, $c)"

:: Kill running Zyntra if any
taskkill /f /im Zyntra.exe >nul 2>&1
timeout /t 2 /nobreak >nul

:: Clean old build output
if exist build\Zyntra.exe del /f /q build\Zyntra.exe >nul 2>&1
if exist build rmdir /s /q build >nul 2>&1

:: Build
echo.
echo Building Zyntra (Release, single-file)...
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

:: Git commit, tag, and push
echo Committing and pushing to GitHub...
git add -A
git commit -m "Release v%VERSION%"
git tag -a "v%VERSION%" -m "v%VERSION%"
git push origin main
git push origin "v%VERSION%"

echo.
echo ========================================
echo   Released v%VERSION% to GitHub!
echo   GitHub Actions will build the release.
echo ========================================
echo.