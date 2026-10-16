# Para a API e o mongod da carga e apaga a pasta de dados temporária.
#   powershell -File parar-ambiente.ps1 -Trabalho <pasta temporária>
param([Parameter(Mandatory)] [string] $Trabalho)

$pids = Join-Path $Trabalho 'pids.txt'
if (Test-Path $pids) {
    foreach ($id in (Get-Content $pids) -split ' ') {
        if ($id) { Stop-Process -Id ([int]$id) -Force -ErrorAction SilentlyContinue }
    }
    Remove-Item $pids
}
Start-Sleep -Seconds 1
$dados = Join-Path $Trabalho 'mongo-dados'
if (Test-Path $dados) { Remove-Item $dados -Recurse -Force -ErrorAction SilentlyContinue }
Write-Output 'ambiente parado'
