$script:Raiz = Split-Path -Parent $PSScriptRoot
$script:CarpetaEjecucion = Join-Path $Raiz '.ejecucion'
$script:ArchivoProcesos = Join-Path $CarpetaEjecucion 'procesos.json'
$script:ArchivoEntorno = Join-Path $Raiz '.env'

function Escribir-Paso([string]$Mensaje) {
    Write-Host "==> $Mensaje" -ForegroundColor Cyan
}

function Escribir-Error([string]$Mensaje) {
    Write-Host $Mensaje -ForegroundColor Red
}

function Obtener-Salida([string]$Comando, [string[]]$Argumentos) {
    $preferencia = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $texto = (& $Comando @Argumentos 2>$null | Out-String).Trim()
        return [pscustomobject]@{ Texto = $texto; Correcto = ($LASTEXITCODE -eq 0) }
    }
    finally {
        $ErrorActionPreference = $preferencia
    }
}

function Ejecutar-Comando([string]$Comando, [string[]]$Argumentos, [string]$MensajeError) {
    $preferencia = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & $Comando @Argumentos | Out-Host
        $codigo = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $preferencia
    }
    if ($codigo -ne 0) {
        throw $MensajeError
    }
}

function Existe-Comando([string]$Nombre) {
    return $null -ne (Get-Command $Nombre -CommandType Application -ErrorAction SilentlyContinue)
}

function Obtener-Variable([string]$Nombre) {
    $valor = [Environment]::GetEnvironmentVariable($Nombre)
    if ($valor) {
        return $valor
    }
    if (Test-Path -LiteralPath $ArchivoEntorno) {
        foreach ($linea in [IO.File]::ReadAllLines($ArchivoEntorno)) {
            if ($linea -match "^\s*$([regex]::Escape($Nombre))\s*=(.*)$") {
                $valor = $Matches[1].Trim().Trim("'", '"')
            }
        }
    }
    return $valor
}

function Leer-Procesos {
    if (-not (Test-Path -LiteralPath $ArchivoProcesos)) {
        return @()
    }
    $datos = Get-Content -LiteralPath $ArchivoProcesos -Raw | ConvertFrom-Json
    return @($datos | ForEach-Object { $_ })
}

function Buscar-Proceso($Registro) {
    $proceso = Get-Process -Id $Registro.Id -ErrorAction SilentlyContinue
    if ($proceso -and [string]$proceso.StartTime.ToFileTimeUtc() -eq [string]$Registro.Inicio) {
        return $proceso
    }
    return $null
}

function Detener-Procesos {
    foreach ($registro in (Leer-Procesos)) {
        if (Buscar-Proceso $registro) {
            Escribir-Paso "Deteniendo $($registro.Nombre)"
            Obtener-Salida 'taskkill.exe' @('/PID', [string]$registro.Id, '/T', '/F') | Out-Null
        }
    }
    if (Test-Path -LiteralPath $ArchivoProcesos) {
        Remove-Item -LiteralPath $ArchivoProcesos -Force
    }
}

function Docker-Responde {
    return (Obtener-Salida 'docker' @('version', '--format', '{{.Server.Version}}')).Correcto
}
