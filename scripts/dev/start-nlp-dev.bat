@echo off
setlocal

set "BACKEND_DIR=%~dp0..\.."
set "NLP_DIR=%BACKEND_DIR%\nlp-service"
set "VENV_PYTHON=venv\Scripts\python.exe"
set "MODEL_DIR=models\intent_classifier"
set "MODEL_FILE=models\intent_classifier\model.safetensors"
set "MODEL_REPO_PATH=nlp-service/models/intent_classifier/model.safetensors"

echo [DEV] EduAI NLP/Vision Service klasorune gidiliyor: %NLP_DIR%
cd /d "%NLP_DIR%" || (
    echo [ERROR] NLP/Vision Service dizini bulunamadi!
    pause
    exit /b 1
)

echo [DEV] Eski NLP/Vision servisleri kontrol ediliyor...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$procs = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'python.exe' -and ($_.CommandLine -like '*uvicorn main:app*' -or $_.CommandLine -like '*nlp-service*') }; foreach ($p in $procs) { Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue }"
if errorlevel 1 (
    echo [ERROR] Eski NLP/Vision servisleri kapatilamadi.
    pause
    exit /b 1
)

where python >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Python bulunamadi! Lutfen Python 3 kurulumunu kontrol edin.
    pause
    exit /b 1
)

if not exist "%VENV_PYTHON%" (
    echo [DEV] [1/4] Python venv olusturuluyor...
    python -m venv venv
    if errorlevel 1 (
        echo [ERROR] Python sanal ortami olusturulamadi.
        pause
        exit /b 1
    )
) else (
    echo [DEV] [1/4] Python venv zaten hazir.
)

echo [DEV] [2/4] Python bagimliliklari yukleniyor/guncelleniyor...
"%VENV_PYTHON%" -m pip install --upgrade pip
if errorlevel 1 (
    echo [ERROR] pip guncellenemedi.
    pause
    exit /b 1
)

"%VENV_PYTHON%" -m pip install -r requirements.txt
if errorlevel 1 (
    echo [ERROR] requirements.txt bagimliliklari yuklenemedi.
    pause
    exit /b 1
)

echo [DEV] [3/4] Model dosyalari kontrol ediliyor...
call :ensure_model
if errorlevel 1 exit /b 1

echo [DEV] [4/4] FastAPI Uvicorn ile baslatiliyor...
echo [INFO] Servis adresi: http://localhost:8000
echo [INFO] Docs: http://localhost:8000/docs
echo.
"%VENV_PYTHON%" -m uvicorn main:app --host 0.0.0.0 --port 8000 --reload

pause
exit /b %ERRORLEVEL%

:ensure_model
set "MODEL_READY=1"
if not exist "%MODEL_FILE%" set "MODEL_READY=0"
if exist "%MODEL_FILE%" (
    findstr /C:"version https://git-lfs.github.com/spec/v1" "%MODEL_FILE%" >nul 2>nul
    if not errorlevel 1 set "MODEL_READY=0"
)

if "%MODEL_READY%"=="0" (
    where git >nul 2>nul
    if not errorlevel 1 (
        echo [DEV] [3/4] Model eksik veya Git LFS pointer durumda, git lfs pull deneniyor...
        git -C "%BACKEND_DIR%" lfs pull --include="%MODEL_REPO_PATH%"
        if errorlevel 1 echo [WARN] git lfs pull basarisiz oldu, MODEL_URL kontrol edilecek.
    )
)

set "MODEL_READY=1"
if not exist "%MODEL_FILE%" set "MODEL_READY=0"
if exist "%MODEL_FILE%" (
    findstr /C:"version https://git-lfs.github.com/spec/v1" "%MODEL_FILE%" >nul 2>nul
    if not errorlevel 1 set "MODEL_READY=0"
)

if "%MODEL_READY%"=="0" (
    if "%MODEL_URL%"=="" (
        echo [ERROR] NLP modeli bulunamadi: %MODEL_FILE%
        echo [INFO] Git LFS ile indirme basarisizsa MODEL_URL environment variable tanimlanmalidir.
        echo [INFO] Ornek: setx MODEL_URL "https://.../model.safetensors"
        pause
        exit /b 1
    )

    if not exist "%MODEL_DIR%" mkdir "%MODEL_DIR%"
    echo [DEV] [3/4] Model bulunamadi, MODEL_URL uzerinden indiriliyor...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Invoke-WebRequest -Uri '%MODEL_URL%' -OutFile '%MODEL_FILE%'"
    if errorlevel 1 (
        echo [ERROR] Model indirilemedi. MODEL_URL degerini ve erisim izinlerini kontrol edin.
        if exist "%MODEL_FILE%" del /q "%MODEL_FILE%" >nul 2>nul
        pause
        exit /b 1
    )
)

exit /b 0
