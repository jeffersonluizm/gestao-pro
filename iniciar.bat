@echo off
title GestaoPro - Iniciador
cd /d "%~dp0"

set "ROOT=%~dp0"
set "API_PORT=5088"
set "WEB_PORT=3000"
set "EXPO_PORT=8081"
set "API_URL=http://0.0.0.0:%API_PORT%"
set "API_LOCAL=http://localhost:%API_PORT%"
set "WEB_URL=http://localhost:%WEB_PORT%"

rem Endereco da API para o celular (EXPO_PUBLIC_API_URL), nesta ordem:
rem 1) variavel EXPO_PUBLIC_API_URL ja definida; 2) src\GestaoPro.Mobile\.env;
rem 3) IPv4 desta maquina na interface com gateway (rede local).
set "API_LAN="
set "API_LAN_ORIGEM="
set "LAN_IP="
if defined EXPO_PUBLIC_API_URL set "API_LAN=%EXPO_PUBLIC_API_URL%"
if defined API_LAN set "API_LAN_ORIGEM=variavel EXPO_PUBLIC_API_URL"
if not defined API_LAN if exist "%ROOT%src\GestaoPro.Mobile\.env" set "API_LAN_ORIGEM=arquivo src\GestaoPro.Mobile\.env"
if not defined API_LAN_ORIGEM for /f "usebackq delims=" %%I in (`powershell -NoProfile -Command "try { (Get-NetIPConfiguration).Where({ $_.IPv4DefaultGateway -and $_.NetAdapter.Status -eq 'Up' })[0].IPv4Address[0].IPAddress } catch { }"`) do set "LAN_IP=%%I"
if defined LAN_IP set "API_LAN=http://%LAN_IP%:%API_PORT%"
if defined LAN_IP set "API_LAN_ORIGEM=IP detectado nesta maquina"
if defined LAN_IP set "EXPO_PUBLIC_API_URL=%API_LAN%"
if not defined API_LAN_ORIGEM set "API_LAN_ORIGEM=nao detectado; crie src\GestaoPro.Mobile\.env (veja .env.example)"

echo ============================================
echo        GestaoPro - Iniciando tudo...
echo ============================================
echo.
echo Abrindo os 3 servidores em janelas separadas.
echo.

netstat -ano | findstr /R /C:":%API_PORT% .*LISTENING" > nul
if errorlevel 1 (
    start "GestaoPro - API" cmd /k "title GestaoPro API && cd /d ""%ROOT%src\GestaoPro.API"" && dotnet run --urls ""%API_URL%"""
) else (
    echo API ja esta rodando na porta %API_PORT%.
)

netstat -ano | findstr /R /C:":%WEB_PORT% .*LISTENING" > nul
if errorlevel 1 (
    start "GestaoPro - Painel Admin" cmd /k "title GestaoPro Painel Admin && cd /d ""%ROOT%src\GestaoPro.Web"" && npm run dev -- --host 0.0.0.0 --port %WEB_PORT%"
) else (
    echo Painel Admin ja esta rodando na porta %WEB_PORT%.
)

netstat -ano | findstr /R /C:":%EXPO_PORT% .*LISTENING" > nul
if errorlevel 1 (
    start "GestaoPro - App Mobile" cmd /k "title GestaoPro App Mobile && cd /d ""%ROOT%src\GestaoPro.Mobile"" && npx expo start --port %EXPO_PORT%"
) else (
    echo App Mobile ja esta rodando na porta %EXPO_PORT%.
)

echo Aguardando os servidores iniciarem (20 segundos)...
timeout /t 20 /nobreak > nul

echo.
echo Abrindo o painel admin no navegador...
start %WEB_URL%

echo.
echo ============================================
echo   GestaoPro esta rodando!
echo.
echo   API local   : %API_LOCAL%
echo   API celular : %API_LAN% (%API_LAN_ORIGEM%)
echo   Swagger     : %API_LOCAL%/swagger
echo   Painel Admin: %WEB_URL%
echo   App Mobile  : escaneie o QR code no terminal Expo
echo ============================================
echo.
pause
