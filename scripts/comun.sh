raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
carpeta_ejecucion="$raiz/.ejecucion"
archivo_procesos="$carpeta_ejecucion/procesos.txt"
archivo_entorno="$raiz/.env"

escribir_paso() {
  printf '\033[36m==> %s\033[0m\n' "$1"
}

escribir_error() {
  printf '\033[31m%s\033[0m\n' "$1" >&2
}

fallar() {
  escribir_error "$1"
  exit 1
}

existe_comando() {
  command -v "$1" >/dev/null 2>&1
}

obtener_variable() {
  local nombre="$1" valor
  valor="$(printenv "$nombre" || true)"
  if [ -z "$valor" ] && [ -f "$archivo_entorno" ]; then
    valor="$(sed -n "s/^[[:space:]]*$nombre[[:space:]]*=//p" "$archivo_entorno" | tail -n 1 |
      tr -d '\r' | sed "s/^[[:space:]]*//; s/[[:space:]]*$//; s/^['\"]//; s/['\"]$//")"
  fi
  printf '%s' "$valor"
}

inicio_proceso() {
  LC_ALL=C ps -o lstart= -p "$1" 2>/dev/null | tr -s ' ' '_' || true
}

proceso_vigente() {
  [ -n "$2" ] && [ "$(inicio_proceso "$1")" = "$2" ]
}

hay_procesos() {
  local nombre identificador inicio
  [ -f "$archivo_procesos" ] || return 1
  while IFS='|' read -r nombre identificador inicio; do
    if proceso_vigente "$identificador" "$inicio"; then
      return 0
    fi
  done < "$archivo_procesos"
  return 1
}

detener_procesos() {
  local nombre identificador inicio intento
  [ -f "$archivo_procesos" ] || return 0
  while IFS='|' read -r nombre identificador inicio; do
    proceso_vigente "$identificador" "$inicio" || continue
    escribir_paso "Deteniendo $nombre"
    kill -TERM -- "-$identificador" 2>/dev/null || true
    for intento in 1 2 3 4 5 6 7 8 9 10; do
      kill -0 -- "-$identificador" 2>/dev/null || break
      sleep 1
    done
    kill -KILL -- "-$identificador" 2>/dev/null || true
  done < "$archivo_procesos"
  rm -f "$archivo_procesos"
}

docker_responde() {
  docker version --format '{{.Server.Version}}' >/dev/null 2>&1
}
