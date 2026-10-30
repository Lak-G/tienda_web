#!/usr/bin/env bash
# Publica TiendaNube desde Linux/macOS/Git Bash: publish linux-x64 self-contained, scp y reinicio por ssh.
# El EC2 no necesita SDK ni runtime de .NET.
#
# Uso: deploy/publicar.sh <ip-o-dns-del-ec2> [llave.pem]
#      USUARIO_SSH=ec2-user por defecto
set -euo pipefail

SERVIDOR="${1:?Uso: $0 <ip-o-dns-del-ec2> [llave.pem]}"
LLAVE="${2:-$HOME/.ssh/tiendanube.pem}"
DESTINO="${USUARIO_SSH:-ec2-user}@$SERVIDOR"

RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
SALIDA="$RAIZ/publish/linux-x64"
PAQUETE="$RAIZ/publish/tiendanube.tar.gz"

rm -rf "$SALIDA"
dotnet publish "$RAIZ/src/TiendaNube.Web" -c Release -r linux-x64 --self-contained -o "$SALIDA"

# Un solo archivo comprimido sube mucho más rápido que cientos de archivos con scp -r
tar -czf "$PAQUETE" -C "$SALIDA" .

scp -i "$LLAVE" "$PAQUETE" "$RAIZ/deploy/instalar-version.sh" "$DESTINO:/tmp/"
ssh -i "$LLAVE" "$DESTINO" 'sudo bash /tmp/instalar-version.sh /tmp/tiendanube.tar.gz'
