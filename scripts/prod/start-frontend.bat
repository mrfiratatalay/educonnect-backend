@echo off
setlocal

set "BACKEND_DIR=%~dp0..\.."
set "WORKSPACE_DIR=%BACKEND_DIR%\.."
set "FRONTEND_DIR=%WORKSPACE_DIR%\frontend"
if not exist "%FRONTEND_DIR%\package.json" set "FRONTEND_DIR=%WORKSPACE_DIR%\TEZ-Frontend"

pushd "%FRONTEND_DIR%"
if errorlevel 1 goto :push_failed

where npm >nul 2>nul
if errorlevel 1 goto :npm_missing

if not exist "node_modules" (
    echo [PROD] Frontend bagimliliklari yukleniyor...
    npm install
    if errorlevel 1 goto :failed
)

echo [PROD] [1/2] Frontend build aliniyor...
npm run build
if errorlevel 1 goto :failed

echo [PROD] [2/2] Frontend preview http://localhost:4173 uzerinde baslatiliyor...
npm run preview -- --host 0.0.0.0 --port 4173
set "EXIT_CODE=%ERRORLEVEL%"
goto :cleanup

:npm_missing
echo npm bulunamadi. Lutfen Node.js kurulumunu kontrol et.
set "EXIT_CODE=1"
set "SHOULD_PAUSE=1"
goto :cleanup

:push_failed
echo Frontend klasoru bulunamadi. Beklenen sibling klasor: frontend veya TEZ-Frontend.
set "EXIT_CODE=1"
set "SHOULD_PAUSE=1"
goto :end

:failed
echo Frontend production baslatma islemi basarisiz oldu.
set "EXIT_CODE=%ERRORLEVEL%"
set "SHOULD_PAUSE=1"
goto :cleanup

:cleanup
popd

:end
if defined SHOULD_PAUSE pause
exit /b %EXIT_CODE%
