@echo off
setlocal

set "SCRIPT_DIR=%~dp0"

echo [PROD] Backend, frontend ve NLP/Vision servisleri ayri pencerelerde baslatiliyor...
start "EduConnect Backend PROD" "%SCRIPT_DIR%start-backend.bat"
start "EduConnect Frontend PROD" "%SCRIPT_DIR%start-frontend.bat"
start "EduAI NLP/Vision PROD" "%SCRIPT_DIR%start-nlp.bat"

exit /b 0
