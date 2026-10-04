# Sobe o ambiente DESCARTÁVEL da carga: mongod local (porta 27100, pasta temporária, 2 threads de CPU)
# e a API publicada (sem appsettings.Local.json). Grava os PIDs em <Trabalho>\pids.txt.
#
#   powershell -File subir-ambiente.ps1 -Api <pasta do dotnet publish> -Porta 5101 -Trabalho <pasta temporária>
param(
    [Parameter(Mandatory)] [string] $Api,
    [Parameter(Mandatory)] [int] $Porta,
    [Parameter(Mandatory)] [string] $Trabalho,
    [string] $Mongod = "$env:USERPROFILE\.cache\mongodb-binaries\mongod-x64-win32-8.2.6.exe"
)
$ErrorActionPreference = 'Stop'

if (Test-Path (Join-Path $Api 'appsettings.Local.json')) {
    throw "Recusado: $Api contém appsettings.Local.json (credencial real). Apague o arquivo da pasta publicada."
}

$dados = Join-Path $Trabalho 'mongo-dados'
if (Test-Path $dados) { Remove-Item $dados -Recurse -Force }
New-Item -ItemType Directory -Path $dados | Out-Null

$mongo = Start-Process -FilePath $Mongod -PassThru -WindowStyle Hidden -ArgumentList @(
    '--port', '27100', '--bind_ip', '127.0.0.1', '--dbpath', "`"$dados`"",
    '--logpath', "`"$(Join-Path $Trabalho 'mongod.log')`"", '--quiet')
Start-Sleep -Milliseconds 500
$mongo.ProcessorAffinity = [IntPtr]0xC00   # threads 10 e 11: separa a CPU do banco da API

$segredo = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 48 | ForEach-Object { [char]$_ })
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = "http://127.0.0.1:$Porta"
$env:MongoDbSettings__ConnectionString = 'mongodb://127.0.0.1:27100'
$env:MongoDbSettings__DatabaseName = 'GestaoProStress'
$env:JwtSettings__SecretKey = $segredo
$env:JwtSettings__ExpirationHours = '4'
$env:Logging__LogLevel__Default = 'Warning'
[Environment]::SetEnvironmentVariable('Logging__LogLevel__Microsoft.AspNetCore', 'Warning')
$env:RateLimiting__Auth__PermitLimit = '1000000'
$env:RateLimiting__Auth__PerCondominiumPermitLimit = '1000000'

Start-Process -FilePath 'dotnet' -WindowStyle Hidden -WorkingDirectory $Api `
    -ArgumentList @('GestaoPro.API.dll', '--contentRoot', "`"$Api`"") `
    -RedirectStandardOutput (Join-Path $Trabalho "api-$Porta.log") -RedirectStandardError (Join-Path $Trabalho "api-$Porta.err")

# O PID da API vem de quem escuta a porta (o Start-Process com redirecionamento nem sempre devolve o processo).
$apiPid = $null
for ($i = 0; $i -lt 60 -and -not $apiPid; $i++) {
    Start-Sleep -Seconds 1
    $apiPid = (Get-NetTCPConnection -LocalPort $Porta -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess
}
if (-not $apiPid) { throw "A API não abriu a porta $Porta. Veja $(Join-Path $Trabalho "api-$Porta.err")." }

"$($mongo.Id) $apiPid" | Set-Content (Join-Path $Trabalho 'pids.txt')
Write-Output "mongod=$($mongo.Id) api=$apiPid"
