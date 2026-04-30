@echo off
setlocal

set "BACKEND_DIR=%~dp0..\.."
pushd "%BACKEND_DIR%"
if errorlevel 1 goto :push_failed

where dotnet >nul 2>nul
if errorlevel 1 goto :dotnet_missing

echo [PROD] [1/3] Restoring backend packages...
dotnet restore "EduConnect.sln"
if errorlevel 1 goto :failed

echo [PROD] [2/3] Building backend...
dotnet build "EduConnect.sln" --no-restore
if errorlevel 1 goto :failed

echo [PROD] [3/3] Starting backend on http://localhost:5160 ...
dotnet run --project "src\EduConnect.Api" --no-build
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

:failed
echo Backend production baslatma islemi basarisiz oldu.
set "EXIT_CODE=%ERRORLEVEL%"
set "SHOULD_PAUSE=1"
goto :cleanup

:cleanup
popd

:end
if defined SHOULD_PAUSE pause
exit /b %EXIT_CODE%
