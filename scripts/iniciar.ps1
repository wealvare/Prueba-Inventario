[CmdletBinding()]
param(
    [switch]$SinNavegador,
    [switch]$SoloBackend,
    [switch]$ConPruebas
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:UseSharedCompilation = 'false'
. (Join-Path $PSScriptRoot 'comun.ps1')

$CarpetaBackend = Join-Path $Raiz 'Backend'
$CarpetaApi = Join-Path $CarpetaBackend 'src\Inventario.Api'
$CarpetaFrontend = Join-Path $Raiz 'Frontend'
$ArchivoAngular = Join-Path $CarpetaFrontend 'node_modules\@angular\cli\bin\ng.js'
$UrlApi = 'http://localhost:5165'
$UrlFrontend = 'http://localhost:4200'
$SegundosBaseDatos = 240
$SegundosServicio = 180
$script:ProcesosIniciados = @()
$script:BaseIniciada = $false

function Convertir-Version([string]$Texto) {
    if ($Texto -match '(\d+)\.(\d+)\.(\d+)') {
        return [version]$Matches[0]
    }
    return $null
}

function Cumple-Rango([string]$Version, [string]$Rango) {
    $actual = Convertir-Version $Version
    foreach ($parte in ($Rango -split '\|\|')) {
        $minima = Convertir-Version $parte
        $mismaMayor = -not $parte.Trim().StartsWith('^') -or $actual.Major -eq $minima.Major
        if ($actual -and $mismaMayor -and $actual -ge $minima) {
            return $true
        }
    }
    return $false
}

function Obtener-ErroresRequisitos {
    $enlaceDocker = 'https://www.docker.com/products/docker-desktop/'
    if (-not (Existe-Comando 'docker')) {
        "Falta Docker con Compose v2. Descarga: $enlaceDocker"
    }
    else {
        $compose = Obtener-Salida 'docker' @('compose', 'version', '--short')
        $versionCompose = Convertir-Version $compose.Texto
        if (-not $compose.Correcto -or -not $versionCompose -or $versionCompose.Major -lt 2) {
            "Docker Compose v2 es obligatorio. Descarga: $enlaceDocker"
        }
        if (-not (Docker-Responde)) {
            "Docker está instalado, pero no responde. Abre Docker Desktop, espera a que termine de iniciar y vuelve a intentarlo. Descarga: $enlaceDocker"
        }
    }

    $enlaceDotnet = 'https://dotnet.microsoft.com/download/dotnet/10.0'
    $versionSdk = [version]((Get-Content -LiteralPath (Join-Path $Raiz 'global.json') -Raw | ConvertFrom-Json).sdk.version)
    if (-not (Existe-Comando 'dotnet')) {
        "Falta el SDK de .NET $versionSdk o posterior de la serie $($versionSdk.Major).$($versionSdk.Minor). Descarga: $enlaceDotnet"
    }
    else {
        $instalados = (Obtener-Salida 'dotnet' @('--list-sdks')).Texto -split "`r?`n" |
            Where-Object { $_ -match '^\d+\.\d+\.\d+\s' } |
            ForEach-Object { Convertir-Version $_ }
        $validos = @($instalados | Where-Object {
                $_.Major -eq $versionSdk.Major -and $_.Minor -eq $versionSdk.Minor -and $_ -ge $versionSdk
            })
        if ($validos.Count -eq 0) {
            "Se requiere el SDK de .NET $versionSdk o posterior de la serie $($versionSdk.Major).$($versionSdk.Minor). Instalados: $(@($instalados) -join ', '). Descarga: $enlaceDotnet"
        }
    }

    if ($SoloBackend) {
        return
    }

    $enlaceNode = 'https://nodejs.org/en/download'
    $motores = (Get-Content -LiteralPath (Join-Path $CarpetaFrontend 'package.json') -Raw | ConvertFrom-Json).engines
    foreach ($herramienta in @(
            @{ Nombre = 'Node.js'; Comando = 'node'; Rango = $motores.node },
            @{ Nombre = 'npm'; Comando = 'npm.cmd'; Rango = $motores.npm })) {
        if (-not (Existe-Comando $herramienta.Comando)) {
            "Falta $($herramienta.Nombre) $($herramienta.Rango). Descarga: $enlaceNode"
            continue
        }
        $version = (Obtener-Salida $herramienta.Comando @('--version')).Texto
        if (-not (Cumple-Rango $version $herramienta.Rango)) {
            "$($herramienta.Nombre) $version no es compatible. Se requiere $($herramienta.Rango). Descarga: $enlaceNode"
        }
    }
}

function Crear-TextoAleatorio([int]$Longitud) {
    $alfabeto = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789'
    $generador = [Security.Cryptography.RandomNumberGenerator]::Create()
    $octeto = New-Object byte[] 1
    $texto = New-Object Text.StringBuilder
    try {
        while ($texto.Length -lt $Longitud) {
            $generador.GetBytes($octeto)
            if ($octeto[0] -lt 224) {
                [void]$texto.Append($alfabeto[$octeto[0] % $alfabeto.Length])
            }
        }
    }
    finally {
        $generador.Dispose()
    }
    return $texto.ToString()
}

function Crear-ContrasenaBase {
    do {
        $contrasena = Crear-TextoAleatorio 32
    } until ($contrasena -cmatch '[A-Z]' -and $contrasena -cmatch '[a-z]' -and $contrasena -match '\d')
    return $contrasena
}

function Preparar-Entorno {
    if (-not (Test-Path -LiteralPath $ArchivoEntorno)) {
        Escribir-Paso 'Creando .env desde .env.example'
        $lineas = [IO.File]::ReadAllLines((Join-Path $Raiz '.env.example')) | ForEach-Object {
            if ($_ -match '^Jwt__Clave=\s*(.*)$' -and ($Matches[1] -eq '' -or $Matches[1] -match 'ejemplo|cambia|clave')) {
                'Jwt__Clave=' + (Crear-TextoAleatorio 64)
            }
            elseif ($_ -match '^BaseDatos__Contrasena=\s*$') {
                'BaseDatos__Contrasena=' + (Crear-ContrasenaBase)
            }
            else {
                $_
            }
        }
        [IO.File]::WriteAllText($ArchivoEntorno, (($lineas -join "`n") + "`n"), (New-Object Text.UTF8Encoding $false))
        Write-Host 'Se generaron la clave JWT y la contraseña de SQL Server que estaban vacías.'
    }

    $vacias = @(
        'BaseDatos__Contrasena',
        'Jwt__Clave',
        'UsuariosIniciales__Administrador__NombreUsuario',
        'UsuariosIniciales__Administrador__Correo',
        'UsuariosIniciales__Administrador__Contrasena',
        'UsuariosIniciales__Consulta__NombreUsuario',
        'UsuariosIniciales__Consulta__Correo',
        'UsuariosIniciales__Consulta__Contrasena') | Where-Object { -not (Obtener-Variable $_) }
    if ($vacias) {
        throw "Completa estas variables en .env: $($vacias -join ', '). Si tu .env es anterior al cambio de nombres, consulta «Migrar un .env existente» en README.md."
    }
}

function Puerto-Ocupado([int]$Puerto) {
    $escuchas = [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
    return @($escuchas | Where-Object { $_.Port -eq $Puerto }).Count -gt 0
}

function Comprobar-Puertos {
    if (Puerto-Ocupado 5165) {
        throw 'El puerto 5165 (Api) está ocupado. Cierra el programa que lo usa, por ejemplo Visual Studio, o ejecuta scripts/detener.ps1.'
    }
    if (-not $SoloBackend -and (Puerto-Ocupado 4200)) {
        throw 'El puerto 4200 (frontend) está ocupado. Cierra el programa que lo usa o ejecuta scripts/detener.ps1.'
    }

    $puerto = Obtener-Variable 'BaseDatos__Puerto'
    if (-not $puerto) {
        $puerto = '1433'
    }
    $publicado = Obtener-Salida 'docker' @('compose', 'port', 'sqlserver', '1433')
    $esPropio = $publicado.Correcto -and $publicado.Texto -match ":$puerto(\s|$)"
    if (-not $esPropio -and (Puerto-Ocupado ([int]$puerto))) {
        throw "El puerto $puerto (SQL Server) está ocupado. Cambia BaseDatos__Puerto en .env por uno libre, por ejemplo 14333, y vuelve a intentarlo."
    }
}

function Iniciar-BaseDatos {
    Escribir-Paso 'Iniciando SQL Server (la primera vez descarga la imagen)'
    $activos = Obtener-Salida 'docker' @('compose', 'ps', '-q', 'sqlserver')
    $script:BaseIniciada = -not $activos.Texto
    Ejecutar-Comando 'docker' @('compose', 'up', '-d') 'No se pudo iniciar SQL Server. Revisa el mensaje anterior de Docker.'
    $contenedor = (Obtener-Salida 'docker' @('compose', 'ps', '-a', '-q', 'sqlserver')).Texto
    $limite = (Get-Date).AddSeconds($SegundosBaseDatos)
    do {
        $estado = (Obtener-Salida 'docker' @('inspect', '--format', '{{.State.Status}}/{{.State.Health.Status}}', $contenedor)).Texto
        if ($estado -eq 'running/healthy') {
            return
        }
        if ($estado -match '^(exited|dead)') {
            break
        }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $limite)

    Ejecutar-Comando 'docker' @('compose', 'logs', '--tail', '15', 'sqlserver') 'No se pudieron leer los registros de SQL Server.'
    throw "SQL Server no quedó listo en $SegundosBaseDatos segundos (estado: $estado). Revisa los registros anteriores y que Docker tenga al menos 2 GB de memoria. Si cambiaste BaseDatos__Contrasena o borraste .env, los datos guardados conservan la contraseña anterior: ejecuta scripts/detener.ps1 -BorrarDatos."
}

function Guardar-Procesos {
    $registros = @($script:ProcesosIniciados | ForEach-Object {
            [pscustomobject]@{ Nombre = $_.Nombre; Id = $_.Id; Inicio = $_.Inicio }
        })
    [IO.File]::WriteAllText($ArchivoProcesos, (ConvertTo-Json -InputObject $registros))
}

function Iniciar-Servicio([string]$Nombre, [string]$Comando, [string[]]$Argumentos, [string]$Carpeta, [string]$Registro) {
    $opciones = @{
        FilePath = (Get-Command $Comando -CommandType Application | Select-Object -First 1).Source
        ArgumentList = @($Argumentos | ForEach-Object { '"' + $_ + '"' })
        WorkingDirectory = $Carpeta
        WindowStyle = 'Hidden'
        RedirectStandardOutput = Join-Path $CarpetaEjecucion "$Registro.log"
        RedirectStandardError = Join-Path $CarpetaEjecucion "$Registro.error.log"
        PassThru = $true
    }
    $proceso = Start-Process @opciones
    [void]$proceso.Handle
    $script:ProcesosIniciados += [pscustomobject]@{
        Nombre = $Nombre
        Id = $proceso.Id
        Inicio = [string]$proceso.StartTime.ToFileTimeUtc()
        Proceso = $proceso
        Registro = $Registro
    }
    Guardar-Procesos
}

function Esperar-Servicio([string]$Url, $Servicio) {
    $limite = (Get-Date).AddSeconds($SegundosServicio)
    while ((Get-Date) -lt $limite -and -not $Servicio.Proceso.HasExited) {
        try {
            if ((Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200) {
                return
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }
    foreach ($sufijo in '.log', '.error.log') {
        $archivo = Join-Path $CarpetaEjecucion ($Servicio.Registro + $sufijo)
        if (Test-Path -LiteralPath $archivo) {
            Get-Content -LiteralPath $archivo -Tail 20 -Encoding UTF8 | Out-Host
        }
    }
    throw "$($Servicio.Nombre) no respondió en $Url. Revisa el registro anterior en la carpeta .ejecucion."
}

function Iniciar-Api {
    Escribir-Paso 'Iniciando la Api'
    $anteriores = @{}
    $valores = @{ ASPNETCORE_ENVIRONMENT = 'Development'; ASPNETCORE_URLS = $UrlApi }
    foreach ($nombre in $valores.Keys) {
        $anteriores[$nombre] = [Environment]::GetEnvironmentVariable($nombre)
        [Environment]::SetEnvironmentVariable($nombre, $valores[$nombre])
    }
    try {
        $ensamblado = Join-Path $CarpetaApi 'bin\Debug\net10.0\Inventario.Api.dll'
        Iniciar-Servicio 'la Api' 'dotnet' @($ensamblado) $CarpetaApi 'api'
    }
    finally {
        foreach ($nombre in $anteriores.Keys) {
            [Environment]::SetEnvironmentVariable($nombre, $anteriores[$nombre])
        }
    }
    Esperar-Servicio "$UrlApi/salud" $script:ProcesosIniciados[-1]
}

function Iniciar-Frontend {
    Escribir-Paso 'Iniciando el frontend'
    $anterior = $env:NG_CLI_ANALYTICS
    $env:NG_CLI_ANALYTICS = 'false'
    try {
        Iniciar-Servicio 'el frontend' 'node' @($ArchivoAngular, 'serve') $CarpetaFrontend 'frontend'
    }
    finally {
        $env:NG_CLI_ANALYTICS = $anterior
    }
    Esperar-Servicio $UrlFrontend $script:ProcesosIniciados[-1]
}

function Preparar-Dependencias {
    Escribir-Paso 'Restaurando y compilando el backend'
    Ejecutar-Comando 'dotnet' @('restore', $CarpetaBackend) 'Falló dotnet restore.'
    Ejecutar-Comando 'dotnet' @('build', $CarpetaBackend, '--no-restore', '--nologo', '-v', 'q') 'Falló la compilación del backend.'
    if (-not $SoloBackend) {
        Escribir-Paso 'Instalando las dependencias del frontend'
        Push-Location $CarpetaFrontend
        try {
            Ejecutar-Comando 'npm.cmd' @('ci', '--no-fund', '--no-audit') 'Falló npm ci.'
        }
        finally {
            Pop-Location
        }
    }
}

function Ejecutar-Pruebas {
    Escribir-Paso 'Ejecutando las pruebas del backend'
    Ejecutar-Comando 'dotnet' @('test', $CarpetaBackend, '--no-build') 'Fallaron las pruebas del backend.'
    if (-not $SoloBackend) {
        Escribir-Paso 'Ejecutando las pruebas del frontend'
        Push-Location $CarpetaFrontend
        try {
            Ejecutar-Comando 'node' @($ArchivoAngular, 'test', '--watch=false') 'Fallaron las pruebas del frontend.'
        }
        finally {
            Pop-Location
        }
    }
}

function Mostrar-Resumen {
    Write-Host ''
    Write-Host 'Todo está en marcha:' -ForegroundColor Green
    if (-not $SoloBackend) {
        Write-Host "  Frontend  $UrlFrontend"
    }
    Write-Host "  Api       $UrlApi/api"
    Write-Host "  Swagger   $UrlApi/swagger"
    Write-Host "  Estado    $UrlApi/salud"
    Write-Host 'Los usuarios de demostración están en las variables UsuariosIniciales__* de .env.'
    Write-Host 'Para detener todo: scripts/detener.ps1'
}

$completado = $false
$codigo = 0
Push-Location $Raiz
try {
    Escribir-Paso 'Comprobando los requisitos'
    $errores = @(Obtener-ErroresRequisitos)
    if ($errores.Count -gt 0) {
        throw ($errores -join [Environment]::NewLine)
    }
    if (@(Leer-Procesos | Where-Object { Buscar-Proceso $_ }).Count -gt 0) {
        throw 'El proyecto ya está en ejecución. Ejecuta antes scripts/detener.ps1.'
    }
    Preparar-Entorno
    Comprobar-Puertos
    [void](New-Item -ItemType Directory -Path $CarpetaEjecucion -Force)
    Iniciar-BaseDatos
    Preparar-Dependencias
    if ($ConPruebas) {
        Ejecutar-Pruebas
    }
    Iniciar-Api
    if (-not $SoloBackend) {
        Iniciar-Frontend
    }
    $completado = $true
    Mostrar-Resumen
    if (-not $SinNavegador) {
        $destino = if ($SoloBackend) { "$UrlApi/swagger" } else { $UrlFrontend }
        try {
            Start-Process $destino
        }
        catch {
            Write-Host "Abre $destino en el navegador."
        }
    }
}
catch {
    $codigo = 1
    Escribir-Error $_.Exception.Message
}
finally {
    if (-not $completado) {
        if ($script:ProcesosIniciados.Count -gt 0) {
            Detener-Procesos
        }
        if ($script:BaseIniciada) {
            Escribir-Paso 'Deteniendo SQL Server (los datos se conservan)'
            Obtener-Salida 'docker' @('compose', 'down') | Out-Null
        }
    }
    Pop-Location
}
exit $codigo
