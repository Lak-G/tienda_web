#!/usr/bin/env bash
# Corre en el EC2; lo invocan publicar.sh y publicar.ps1 por ssh.
# Instala el paquete publicado en /opt/tiendanube/app, reinicia el servicio y espera a que responda.
# La versión previa queda en /opt/tiendanube/anterior para revertir (ver README).
set -euo pipefail

PAQUETE="${1:-/tmp/tiendanube.tar.gz}"
BASE=/opt/tiendanube

rm -rf "$BASE/nueva"
mkdir -p "$BASE/nueva"
tar -xzf "$PAQUETE" -C "$BASE/nueva" --no-same-owner
chown -R root:root "$BASE/nueva"
chmod -R a+rX "$BASE/nueva"
chmod +x "$BASE/nueva/TiendaNube.Web"

systemctl stop tiendanube || true
rm -rf "$BASE/anterior"
if [[ -d "$BASE/app" ]]; then
    mv "$BASE/app" "$BASE/anterior"
fi
mv "$BASE/nueva" "$BASE/app"
systemctl start tiendanube

for _ in $(seq 1 30); do
    if curl -fs -o /dev/null http://127.0.0.1:5000/; then
        rm -f "$PAQUETE"
        echo "TiendaNube en línea."
        exit 0
    fi
    sleep 1
done

echo "El servicio no respondió en 30 s. Últimas líneas del log:" >&2
journalctl -u tiendanube -n 40 --no-pager >&2
exit 1
