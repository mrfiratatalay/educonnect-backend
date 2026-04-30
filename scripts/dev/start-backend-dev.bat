@echo off
setlocal

set "BACKEND_DIR=%~dp0..\.."
pushd "%BACKEND_DIR%"
if errorlevel 1 goto :push_failed

where dotnet >nul 2>nul
if errorlevel 1 goto :dotnet_missing

echo [DEV] Eski backend/watch surecleri kontrol ediliyor...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$procs = Get-CimInstance Win32_Process | Where-Object { ($_.Name -eq 'EduConnect.Api.exe') -or ($_.Name -eq 'dotnet.exe' -and ($_.CommandLine -like '*src\EduConnect.Api*' -or $_.CommandLine -like '*src/EduConnect.Api*')) }; foreach ($p in $procs) { Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue }"
if errorlevel 1 goto :cleanup

echo [DEV] Starting backend with dotnet watch on http://localhost:5160 ...
dotnet watch --project "src\EduConnect.Api" run
set "EXIT_CODE=%ERRORLEVEL%"
goto :cleanup

:dotnet_missing
echo dotnet CLI bulunamadi. Lutfen .NET SDK kurulumunu kontrol et.
set "EXIT_CODE=1"
set "SHOULD_PAUSE=1"
goto :cleanup

:push_failed
echo Backend klasorune gecilemedi.
set "EXIT_CODE=1"
set "SHOULD_PAUSE=1"
goto :end

:cleanup
popd

:end
if defined SHOULD_PAUSE pause
exit /b %EXIT_CODE%
