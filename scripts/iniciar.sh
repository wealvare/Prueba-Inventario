#!/usr/bin/env bash
set -euo pipefail
. "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/comun.sh"

export MSBUILDDISABLENODEREUSE=1
export UseSharedCompilation=false
sin_navegador=false
solo_backend=false
con_pruebas=false
url_api='http://localhost:5165'
url_frontend='http://localhost:4200'
carpeta_backend="$raiz/Backend"
carpeta_api="$carpeta_backend/src/Inventario.Api"
carpeta_frontend="$raiz/Frontend"
archivo_angular="$carpeta_frontend/node_modules/@angular/cli/bin/ng.js"
segundos_base_datos=240
segundos_servicio=180
procesos_iniciados=false
base_iniciada=false
completado=false
ultimo_proceso=''

for opcion in "$@"; do
  case "$opcion" in
    --sin-navegador) sin_navegador=true ;;
    --solo-backend) solo_backend=true ;;
    --con-pruebas) con_pruebas=true ;;
    *) fallar "Opción no válida: $opcion. Usa --sin-navegador, --solo-backend o --con-pruebas." ;;
  esac
done

limpiar() {
  local codigo=$?
  trap - EXIT
  trap '' INT TERM
  if [ "$completado" = false ]; then
    if [ "$procesos_iniciados" = true ]; then
      detener_procesos
    fi
    if [ "$base_iniciada" = true ]; then
      escribir_paso 'Deteniendo SQL Server (los datos se conservan)'
      docker compose down >/dev/null 2>&1 || true
    fi
  fi
  exit "$codigo"
}

version_sdk_valida() {
  awk -v minima="$1" '
    BEGIN { split(minima, m, ".") }
    $1 ~ /^[0-9]+\.[0-9]+\.[0-9]+$/ {
      split($1, v, ".")
      if (v[1] == m[1] && v[2] == m[2] && v[3] + 0 >= m[3] + 0) encontrado = 1
    }
    END { exit encontrado ? 0 : 1 }'
}

cumple_rango() {
  node -e '
    const [version, rango] = process.argv.slice(1);
    const numeros = (texto) => texto.replace(/[^0-9.]/g, "").split(".").map(Number);
    const comparar = (a, b) => a[0] - b[0] || a[1] - b[1] || a[2] - b[2];
    const actual = numeros(version);
    const cumple = rango.split("||").some((parte) => {
      const minima = numeros(parte);
      const mismaMayor = !parte.trim().startsWith("^") || actual[0] === minima[0];
      return mismaMayor && comparar(actual, minima) >= 0;
    });
    process.exit(cumple ? 0 : 1);' "$1" "$2"
}

comprobar_requisitos() {
  local errores='' enlace_docker='https://docs.docker.com/get-docker/'
  local enlace_dotnet='https://dotnet.microsoft.com/download/dotnet/10.0'
  local enlace_node='https://nodejs.org/en/download'
  local version_compose version_sdk serie rango_node rango_npm
  local salto=$'\n'

  if ! existe_comando docker; then
    errores+="Falta Docker con Compose v2. Descarga: $enlace_docker$salto"
  else
    version_compose="$(docker compose version --short 2>/dev/null || true)"
    version_compose="${version_compose#v}"
    case "${version_compose%%.*}" in
      ''|*[!0-9]*|0|1) errores+="Docker Compose v2 es obligatorio. Descarga: $enlace_docker$salto" ;;
    esac
    if ! docker_responde; then
      errores+="Docker está instalado, pero no responde. Inicia Docker, comprueba que tu usuario pueda usarlo y vuelve a intentarlo. Descarga: $enlace_docker$salto"
    fi
  fi

  version_sdk="$(sed -n 's/.*"version"[^0-9]*\([0-9.]*\).*/\1/p' "$raiz/global.json")"
  serie="${version_sdk%.*}"
  if ! existe_comando dotnet; then
    errores+="Falta el SDK de .NET $version_sdk o posterior de la serie $serie. Descarga: $enlace_dotnet$salto"
  elif ! (dotnet --list-sdks 2>/dev/null || true) | version_sdk_valida "$version_sdk"; then
    errores+="Se requiere el SDK de .NET $version_sdk o posterior de la serie $serie. Descarga: $enlace_dotnet$salto"
  fi

  if [ "$solo_backend" = false ]; then
    rango_node="$(sed -n '/"engines"/,/}/s/.*"node": "\([^"]*\)".*/\1/p' "$carpeta_frontend/package.json")"
    rango_npm="$(sed -n '/"engines"/,/}/s/.*"npm": "\([^"]*\)".*/\1/p' "$carpeta_frontend/package.json")"
    if ! existe_comando node; then
      errores+="Falta Node.js $rango_node. Descarga: $enlace_node$salto"
    elif ! cumple_rango "$(node --version)" "$rango_node"; then
      errores+="Node.js $(node --version) no es compatible. Se requiere $rango_node. Descarga: $enlace_node$salto"
    fi
    if ! existe_comando npm; then
      errores+="Falta npm $rango_npm. Descarga: $enlace_node$salto"
    elif existe_comando node && ! cumple_rango "$(npm --version)" "$rango_npm"; then
      errores+="npm $(npm --version) no es compatible. Se requiere $rango_npm. Descarga: $enlace_node$salto"
    fi
  fi

  if [ -n "$errores" ]; then
    fallar "${errores%"$salto"}"
  fi
}

crear_texto_aleatorio() {
  local longitud="$1" texto=''
  while [ "${#texto}" -lt "$longitud" ]; do
    texto+="$(head -c 256 /dev/urandom | LC_ALL=C tr -dc 'A-HJ-NP-Za-km-z2-9')"
  done
  printf '%s' "${texto:0:$longitud}"
}

crear_contrasena_base() {
  local contrasena
  while true; do
    contrasena="$(crear_texto_aleatorio 32)"
    case "$contrasena" in *[A-Z]*) ;; *) continue ;; esac
    case "$contrasena" in *[a-z]*) ;; *) continue ;; esac
    case "$contrasena" in *[0-9]*) ;; *) continue ;; esac
    printf '%s' "$contrasena"
    return
  done
}

preparar_entorno() {
  local clave contrasena variable vacias='' temporal="$archivo_entorno.tmp"
  if [ ! -f "$archivo_entorno" ]; then
    escribir_paso 'Creando .env desde .env.example'
    clave="$(crear_texto_aleatorio 64)"
    contrasena="$(crear_contrasena_base)"
    (
      umask 077
      tr -d '\r' < "$raiz/.env.example" | awk -v clave="$clave" -v contrasena="$contrasena" '
        /^Jwt__Clave=/ {
          valor = tolower(substr($0, 12))
          if (valor ~ /^[ \t]*$/ || valor ~ /ejemplo|cambia|clave/) { print "Jwt__Clave=" clave; next }
        }
        /^BaseDatos__Contrasena=[ \t]*$/ { print "BaseDatos__Contrasena=" contrasena; next }
        { print }' > "$temporal"
    )
    mv "$temporal" "$archivo_entorno"
    echo 'Se generaron la clave JWT y la contraseña de SQL Server que estaban vacías.'
  fi

  for variable in BaseDatos__Contrasena Jwt__Clave \
    UsuariosIniciales__Administrador__NombreUsuario UsuariosIniciales__Administrador__Correo \
    UsuariosIniciales__Administrador__Contrasena \
    UsuariosIniciales__Consulta__NombreUsuario UsuariosIniciales__Consulta__Correo \
    UsuariosIniciales__Consulta__Contrasena; do
    if [ -z "$(obtener_variable "$variable")" ]; then
      vacias+=" $variable"
    fi
  done
  if [ -n "$vacias" ]; then
    fallar "Completa estas variables en .env:$vacias. Si tu .env es anterior al cambio de nombres, renómbralas según .env.example."
  fi
}

puerto_ocupado() {
  (exec 3<>"/dev/tcp/127.0.0.1/$1") 2>/dev/null || (exec 3<>"/dev/tcp/::1/$1") 2>/dev/null
}

comprobar_puertos() {
  local puerto publicado
  if puerto_ocupado 5165; then
    fallar 'El puerto 5165 (Api) está ocupado. Cierra el programa que lo usa o ejecuta scripts/detener.sh.'
  fi
  if [ "$solo_backend" = false ] && puerto_ocupado 4200; then
    fallar 'El puerto 4200 (frontend) está ocupado. Cierra el programa que lo usa o ejecuta scripts/detener.sh.'
  fi
  puerto="$(obtener_variable BaseDatos__Puerto)"
  puerto="${puerto:-1433}"
  publicado="$(docker compose port sqlserver 1433 2>/dev/null || true)"
  case "$publicado" in
    *":$puerto") ;;
    *)
      if puerto_ocupado "$puerto"; then
        fallar "El puerto $puerto (SQL Server) está ocupado. Cambia BaseDatos__Puerto en .env por uno libre, por ejemplo 14333, y vuelve a intentarlo."
      fi
      ;;
  esac
}

iniciar_base_datos() {
  local contenedor estado='' limite
  escribir_paso 'Iniciando SQL Server (la primera vez descarga la imagen)'
  if [ -z "$(docker compose ps -q sqlserver 2>/dev/null || true)" ]; then
    base_iniciada=true
  fi
  docker compose up -d || fallar 'No se pudo iniciar SQL Server. Revisa el mensaje anterior de Docker.'
  contenedor="$(docker compose ps -a -q sqlserver)"
  limite=$((SECONDS + segundos_base_datos))
  while [ "$SECONDS" -lt "$limite" ]; do
    estado="$(docker inspect --format '{{.State.Status}}/{{.State.Health.Status}}' "$contenedor" 2>/dev/null || true)"
    case "$estado" in
      running/healthy) return ;;
      exited*|dead*) break ;;
    esac
    sleep 2
  done
  docker compose logs --tail 15 sqlserver || true
  fallar "SQL Server no quedó listo en $segundos_base_datos segundos (estado: $estado). Revisa los registros anteriores y que Docker tenga al menos 2 GB de memoria. Si cambiaste BaseDatos__Contrasena o borraste .env, los datos guardados conservan la contraseña anterior: ejecuta scripts/detener.sh --borrar-datos."
}

preparar_dependencias() {
  escribir_paso 'Restaurando y compilando el backend'
  dotnet restore "$carpeta_backend" || fallar 'Falló dotnet restore.'
  dotnet build "$carpeta_backend" --no-restore --nologo -v q || fallar 'Falló la compilación del backend.'
  if [ "$solo_backend" = false ]; then
    escribir_paso 'Instalando las dependencias del frontend'
    (cd "$carpeta_frontend" && npm ci --no-fund --no-audit) || fallar 'Falló npm ci.'
  fi
}

ejecutar_pruebas() {
  escribir_paso 'Ejecutando las pruebas del backend'
  dotnet test "$carpeta_backend" --no-build || fallar 'Fallaron las pruebas del backend.'
  if [ "$solo_backend" = false ]; then
    escribir_paso 'Ejecutando las pruebas del frontend'
    (cd "$carpeta_frontend" && node "$archivo_angular" test --watch=false) || fallar 'Fallaron las pruebas del frontend.'
  fi
}

iniciar_servicio() {
  local nombre="$1" carpeta="$2" registro="$3"
  shift 3
  set -m
  (cd "$carpeta" && exec nohup "$@") > "$carpeta_ejecucion/$registro.log" 2>&1 < /dev/null &
  ultimo_proceso=$!
  set +m
  procesos_iniciados=true
  printf '%s|%s|%s\n' "$nombre" "$ultimo_proceso" "$(inicio_proceso "$ultimo_proceso")" >> "$archivo_procesos"
}

responde() {
  local puerto="$1" ruta="$2" servidor estado
  for servidor in 127.0.0.1 ::1; do
    estado="$( (
      exec 3<>"/dev/tcp/$servidor/$puerto" || exit 1
      printf 'GET %s HTTP/1.0\r\nHost: localhost:%s\r\nAccept: text/html\r\n\r\n' "$ruta" "$puerto" >&3
      IFS= read -r -t 5 linea <&3
      printf '%s' "$linea"
    ) 2>/dev/null || true)"
    case "$estado" in
      HTTP/*' 200'*) return 0 ;;
    esac
  done
  return 1
}

esperar_servicio() {
  local nombre="$1" puerto="$2" ruta="$3" registro="$4" limite=$((SECONDS + segundos_servicio))
  while [ "$SECONDS" -lt "$limite" ] && kill -0 "$ultimo_proceso" 2>/dev/null; do
    if responde "$puerto" "$ruta"; then
      return
    fi
    sleep 1
  done
  tail -n 20 "$carpeta_ejecucion/$registro.log" || true
  fallar "$nombre no respondió en http://localhost:$puerto$ruta. Revisa el registro anterior en la carpeta .ejecucion."
}

iniciar_api() {
  escribir_paso 'Iniciando la Api'
  iniciar_servicio 'la Api' "$carpeta_api" api \
    env ASPNETCORE_ENVIRONMENT=Development "ASPNETCORE_URLS=$url_api" \
    dotnet "$carpeta_api/bin/Debug/net10.0/Inventario.Api.dll"
  esperar_servicio 'La Api' 5165 /salud api
}

iniciar_frontend() {
  escribir_paso 'Iniciando el frontend'
  iniciar_servicio 'el frontend' "$carpeta_frontend" frontend \
    env NG_CLI_ANALYTICS=false node "$archivo_angular" serve
  esperar_servicio 'El frontend' 4200 / frontend
}

mostrar_resumen() {
  printf '\n\033[32mTodo está en marcha:\033[0m\n'
  if [ "$solo_backend" = false ]; then
    echo "  Frontend  $url_frontend"
  fi
  echo "  Api       $url_api/api"
  echo "  Swagger   $url_api/swagger"
  echo "  Estado    $url_api/salud"
  echo 'Los usuarios de demostración están en las variables UsuariosIniciales__* de .env.'
  echo 'Para detener todo: scripts/detener.sh'
}

abrir_navegador() {
  local destino="$url_frontend"
  if [ "$solo_backend" = true ]; then
    destino="$url_api/swagger"
  fi
  if existe_comando open; then
    open "$destino" >/dev/null 2>&1 || true
  elif existe_comando xdg-open; then
    xdg-open "$destino" >/dev/null 2>&1 || true
  else
    echo "Abre $destino en el navegador."
  fi
}

trap limpiar EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

cd "$raiz"
escribir_paso 'Comprobando los requisitos'
comprobar_requisitos
if hay_procesos; then
  fallar 'El proyecto ya está en ejecución. Ejecuta antes scripts/detener.sh.'
fi
preparar_entorno
comprobar_puertos
mkdir -p "$carpeta_ejecucion"
rm -f "$archivo_procesos"
iniciar_base_datos
preparar_dependencias
if [ "$con_pruebas" = true ]; then
  ejecutar_pruebas
fi
iniciar_api
if [ "$solo_backend" = false ]; then
  iniciar_frontend
fi
completado=true
mostrar_resumen
if [ "$sin_navegador" = false ]; then
  abrir_navegador
fi
