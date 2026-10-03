$buildId = '4bb8b445-c104-476e-b098-276c4f6c2832'
while ($true) {
    try {
        $json = npm exec --yes --package eas-cli -- eas build:view $buildId --json | Out-String
        if ([string]::IsNullOrWhiteSpace($json)) {
            Write-Host "$(Get-Date -Format o) - sem resposta do eas"
        } else {
            $obj = $json | ConvertFrom-Json -ErrorAction SilentlyContinue
            if ($null -eq $obj) {
                Write-Host "$(Get-Date -Format o) - resposta não-JSON"
                Write-Host $json
            } else {
                $status = $obj.status
                Write-Host "$(Get-Date -Format o) - Status: $status"
                if ($status -in @('finished','errored','canceled','failed')) {
                    Write-Host "Build finalizado: $status"
                    Write-Host $json
                    break
                }
            }
        }
    } catch {
        Write-Host "$(Get-Date -Format o) - erro: $_"
    }
    Start-Sleep -Seconds 20
}
