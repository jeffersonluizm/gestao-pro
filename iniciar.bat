@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Gestao Pro - iniciar
cd /d "%~dp0"
set "ROOT=%~dp0"

rem ==========================================================================
rem  Gestao Pro: sobe o banco (Docker), a API e o painel web na maquina local,
rem  com o modo demonstracao ligado. Pode rodar quantas vezes quiser.
rem  Uso: iniciar.bat          (API + painel web)
rem       iniciar.bat app      (tambem abre o app Expo)
rem ==========================================================================

set "API_PORT=5088"
set "WEB_PORT=3000"
set "API_URL=http://localhost:%API_PORT%"
set "WEB_URL=http://localhost:%WEB_PORT%"

echo.
echo  ===  Gestao Pro  ===
echo.

rem --- 1. Pre-requisitos -----------------------------------------------------
where dotnet >nul 2>&1 || (call :falta ".NET 10 SDK" "https://dotnet.microsoft.com/download/dotnet/10.0" & goto :fim)
for /f "tokens=1 delims=." %%v in ('dotnet --version') do set "DOTNET_MAJOR=%%v"
if !DOTNET_MAJOR! LSS 10 (call :falta ".NET 10 SDK (encontrado .NET !DOTNET_MAJOR!)" "https://dotnet.microsoft.com/download/dotnet/10.0" & goto :fim)
where node >nul 2>&1 || (call :falta "Node.js 18 ou mais novo" "https://nodejs.org/" & goto :fim)
where docker >nul 2>&1 || (call :falta "Docker Desktop (para o banco local)" "https://www.docker.com/products/docker-desktop/" & goto :fim)
docker info >nul 2>&1 || (echo  [x] O Docker esta instalado, mas nao esta rodando. Abra o Docker Desktop e rode de novo. & goto :fim)
echo  [ok] .NET !DOTNET_MAJOR!, Node e Docker encontrados.

rem --- 2. Configuracao local (.env) com segredos fortes -----------------------
if not exist ".env" (
  copy /y ".env.example" ".env" >nul
  echo  [ok] .env criado a partir do .env.example.
)
powershell -NoProfile -Command ^
  "$f='.env'; $t=Get-Content $f -Raw;" ^
  "function S($p){ $a='abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789'; $p + '-' + (-join (1..14 | %% { $a[(Get-Random -Maximum $a.Length)] })) + '-2026' }" ^
  "$t=$t -replace 'JwtSettings__SecretKey=gerar', ('JwtSettings__SecretKey=' + (S 'Jwt') + (S 'Chave'));" ^
  "$t=$t -replace 'Demo__Senha=gerar', ('Demo__Senha=' + (S 'Demo'));" ^
  "$t=$t -replace 'Demo__CodigoCondominio=gerar', ('Demo__CodigoCondominio=' + (S 'Codigo'));" ^
  "Set-Content -Path $f -Value $t -NoNewline -Encoding ascii"
for /f "usebackq eol=# tokens=1,* delims==" %%A in (".env") do if not "%%A"=="" set "%%A=%%B"
echo  [ok] Configuracao local carregada (segredos so no .env, fora do Git).

rem --- 3. Banco local --------------------------------------------------------
echo  [..] Subindo o MongoDB local (docker compose)...
docker compose up -d --wait mongo >nul 2>&1 || (echo  [x] Nao foi possivel subir o MongoDB. Veja "docker compose logs mongo". & goto :fim)
echo  [ok] MongoDB local pronto na porta 27117.

rem --- 4. Dependencias do painel ---------------------------------------------
if not exist "src\GestaoPro.Web\node_modules" (
  echo  [..] Instalando dependencias do painel (primeira vez)...
  pushd "src\GestaoPro.Web" & call npm install --no-audit --no-fund >nul 2>&1 & popd
)

rem --- 5. API e painel (janelas separadas, so se ainda nao estiverem no ar) -----
netstat -ano | findstr /R /C:":%API_PORT% .*LISTENING" >nul
if errorlevel 1 (
  start "Gestao Pro - API" cmd /k "cd /d ""%ROOT%src\GestaoPro.API"" && set ASPNETCORE_ENVIRONMENT=Development&& dotnet run --urls %API_URL%"
) else (echo  [ok] A API ja esta rodando na porta %API_PORT%.)

netstat -ano | findstr /R /C:":%WEB_PORT% .*LISTENING" >nul
if errorlevel 1 (
  start "Gestao Pro - Painel" cmd /k "cd /d ""%ROOT%src\GestaoPro.Web"" && npm run dev -- --port %WEB_PORT%"
) else (echo  [ok] O painel ja esta rodando na porta %WEB_PORT%.)

if /i "%~1"=="app" (
  if not exist "src\GestaoPro.Mobile\node_modules" (pushd "src\GestaoPro.Mobile" & call npm install --no-audit --no-fund >nul 2>&1 & popd)
  start "Gestao Pro - App" cmd /k "cd /d ""%ROOT%src\GestaoPro.Mobile"" && set EXPO_PUBLIC_API_URL=%API_URL%&& npx expo start --web"
)

rem --- 6. Espera a API responder (health) ------------------------------------
echo  [..] Esperando a API responder (a primeira compilacao leva cerca de 1 minuto)...
set /a TENTATIVAS=0
:espera
set /a TENTATIVAS+=1
powershell -NoProfile -Command "try { (Invoke-WebRequest -UseBasicParsing %API_URL%/api/health/mongodb -TimeoutSec 3).StatusCode } catch { 0 }" | findstr "200" >nul
if errorlevel 1 (
  if !TENTATIVAS! GEQ 90 (echo  [x] A API nao respondeu em 3 minutos. Veja a janela "Gestao Pro - API". & goto :fim)
  timeout /t 2 /nobreak >nul
  goto :espera
)
echo  [ok] API no ar.

start "" %WEB_URL%

echo.
echo  ==========================================================
echo   Gestao Pro rodando
echo     Painel   : %WEB_URL%   (clique em "Entrar como ...")
echo     API      : %API_URL%   (documentacao em %API_URL%/swagger)
echo     Senha das contas demo     : %Demo__Senha%
echo     Codigo do condominio demo : %Demo__CodigoCondominio%
echo   Para o app: iniciar.bat app
echo   Para parar: feche as janelas e rode "docker compose stop".
echo  ==========================================================

:fim
echo.
pause
exit /b

:falta
echo  [x] Falta %~1. Instale em %~2 e rode o iniciar.bat de novo.
exit /b
