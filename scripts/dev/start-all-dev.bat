@echo off
setlocal

set "SCRIPT_DIR=%~dp0"

echo [DEV] Servisler ayri pencerelerde baslatiliyor...
start "EduConnect Backend (C#)" "%SCRIPT_DIR%start-backend-dev.bat"
start "EduConnect Frontend (React)" "%SCRIPT_DIR%start-frontend-dev.bat"
start "EduAI NLP/Vision Service (Python)" "%SCRIPT_DIR%start-nlp-dev.bat"

exit /b 0
