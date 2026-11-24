[CmdletBinding()]
param(
    [switch]$BorrarDatos
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'comun.ps1')

$codigo = 0
$contrasenaTemporal = $false
Push-Location $Raiz
try {
    Detener-Procesos
    if (-not (Existe-Comando 'docker') -or -not (Docker-Responde)) {
        throw 'Docker no responde, así que no se pudo detener SQL Server. Abre Docker Desktop y vuelve a ejecutar el script.'
    }
    if (-not (Obtener-Variable 'BaseDatos__Contrasena')) {
        $env:BaseDatos__Contrasena = [guid]::NewGuid().ToString()
        $contrasenaTemporal = $true
    }
    $argumentos = @('compose', 'down')
    if ($BorrarDatos) {
        $argumentos += '-v'
    }
    Escribir-Paso 'Deteniendo SQL Server'
    Ejecutar-Comando 'docker' $argumentos 'No se pudo detener SQL Server.'
    if ($BorrarDatos) {
        Write-Host 'Todo detenido. Se borraron los datos.' -ForegroundColor Green
    }
    else {
        Write-Host 'Todo detenido. Los datos se conservan.' -ForegroundColor Green
    }
}
catch {
    $codigo = 1
    Escribir-Error $_.Exception.Message
}
finally {
    if ($contrasenaTemporal) {
        Remove-Item Env:BaseDatos__Contrasena
    }
    Pop-Location
}
exit $codigo
