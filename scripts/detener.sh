#!/usr/bin/env bash
set -euo pipefail
. "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/comun.sh"

borrar_datos=false
for opcion in "$@"; do
  case "$opcion" in
    --borrar-datos) borrar_datos=true ;;
    *) fallar "Opción no válida: $opcion. Usa --borrar-datos." ;;
  esac
done

cd "$raiz"
detener_procesos
if ! existe_comando docker || ! docker_responde; then
  fallar 'Docker no responde, así que no se pudo detener SQL Server. Inicia Docker y vuelve a ejecutar el script.'
fi
if [ -z "$(obtener_variable BaseDatos__Contrasena)" ]; then
  contrasena_temporal="temporal-$$-$RANDOM"
  entorno=(env "BaseDatos__Contrasena=$contrasena_temporal")
else
  entorno=(env)
fi
argumentos=(compose down)
if [ "$borrar_datos" = true ]; then
  argumentos+=(-v)
fi
escribir_paso 'Deteniendo SQL Server'
"${entorno[@]}" docker "${argumentos[@]}" || fallar 'No se pudo detener SQL Server.'
if [ "$borrar_datos" = true ]; then
  echo 'Todo detenido. Se borraron los datos.'
else
  echo 'Todo detenido. Los datos se conservan.'
fi
