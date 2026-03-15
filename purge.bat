@echo off
setlocal

:: Retrieves the path of the executable passed as an argument
set "APP_EXE=%~1"

:: Wait until CookieVault.exe is completely closed (5 seconds max).
echo Waiting for CookieVault to shut down...
timeout /t 2 /nobreak >nul

:WAIT_LOOP
tasklist /FI "IMAGENAME eq CookieVault.exe" 2>nul | find /I "CookieVault.exe" >nul
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto WAIT_LOOP
)

:: Deletion of the WebData directory
echo Purge in progress...
rd /s /q "%LOCALAPPDATA%\CookieVault\WebData"

if exist "%LOCALAPPDATA%\CookieVault\WebData" (
    echo ERROR : The deletion failed..
    pause
    exit /b 1
)

echo Purge successfully completed.
timeout /t 1 /nobreak >nul

:: Restarting the application
echo Restart CookieVault...
start "" "%APP_EXE%"

exit