#!/usr/bin/env bash
# Corre en el EC2: aplica migraciones.sql (idempotente) contra RDS usando la cadena de conexión
# de /etc/tiendanube/tiendanube.env, sin escribir la contraseña en la línea de comandos ni en el historial.
#
# Uso: sudo bash aplicar-migraciones.sh [ruta/migraciones.sql]
#      sudo bash aplicar-migraciones.sh --probar     (solo verifica la conexión)
set -euo pipefail

ARCHIVO_ENV=/etc/tiendanube/tiendanube.env
SCRIPT="${1:-$(dirname "$0")/migraciones.sql}"

set -a
# shellcheck disable=SC1090
source "$ARCHIVO_ENV"
set +a

# Traduce la cadena de Npgsql (Clave=Valor;...) a las variables PG* que entiende psql
IFS=';' read -ra partes <<< "${ConnectionStrings__Default:?Falta ConnectionStrings__Default en $ARCHIVO_ENV}"
for parte in "${partes[@]}"; do
    [[ -z "$parte" ]] && continue
    clave="$(echo "${parte%%=*}" | tr -d ' ' | tr '[:upper:]' '[:lower:]')"
    valor="${parte#*=}"
    case "$clave" in
        host|server) export PGHOST="$valor" ;;
        port) export PGPORT="$valor" ;;
        database) export PGDATABASE="$valor" ;;
        username|userid|user) export PGUSER="$valor" ;;
        password) export PGPASSWORD="$valor" ;;
        sslmode) export PGSSLMODE="$(echo "$valor" | tr '[:upper:]' '[:lower:]')" ;;
    esac
done
export PGSSLMODE="${PGSSLMODE:-require}"
export PGCONNECT_TIMEOUT=10

if [[ "$SCRIPT" == "--probar" ]]; then
    psql -At -c "select 'Conectado a ' || current_database() || ' como ' || current_user || ', SSL: ' || (select ssl from pg_stat_ssl where pid = pg_backend_pid()) || ', ' || version();"
    exit 0
fi

psql -v ON_ERROR_STOP=1 -q -f "$SCRIPT"
psql -At -c 'select '"'"'Migraciones aplicadas: '"'"' || string_agg("MigrationId", '"'"', '"'"' order by "MigrationId") from "__EFMigrationsHistory";'
