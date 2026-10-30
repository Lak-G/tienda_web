#!/usr/bin/env bash
# Prepara un EC2 Amazon Linux 2023 para TiendaNube.
#
# La instancia actual ya está configurada; este script documenta los pasos para replicarla.
# Es idempotente: correrlo de nuevo solo agrega lo que falte (no sobrescribe el .env existente).
# La base de datos vive en Amazon RDS, así que aquí solo se instala el cliente psql para las migraciones.
#
# Uso (desde la carpeta deploy/ copiada al EC2):  sudo bash setup-ec2.sh
set -euo pipefail

cd "$(dirname "$0")"

if [[ $EUID -ne 0 ]]; then
    echo "Ejecuta con sudo." >&2
    exit 1
fi

echo "== Swap de 1 GB (la t3.micro solo tiene 1 GB de RAM; el publish y los picos de memoria la necesitan)"
if ! swapon --show | grep -q '^/swapfile'; then
    dd if=/dev/zero of=/swapfile bs=1M count=1024 status=none
    chmod 600 /swapfile
    mkswap /swapfile >/dev/null
    swapon /swapfile
fi
grep -q '^/swapfile' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab

echo "== Paquetes"
# nginx: proxy inverso en el puerto 80
# postgresql16: solo el cliente psql, para aplicar migraciones contra RDS
# libicu: la app usa la cultura es-MX; sin ICU un ejecutable .NET self-contained no arranca
dnf install -y nginx postgresql16 libicu tar

echo "== Usuario de servicio y directorios"
id tiendanube &>/dev/null || useradd --system --no-create-home --shell /sbin/nologin tiendanube
install -d -m 755 /opt/tiendanube
# Claves de Data Protection (cookies de admin, sesión y antiforgery); deben sobrevivir a los reinicios
install -d -o tiendanube -g tiendanube -m 700 /var/lib/tiendanube /var/lib/tiendanube/claves
install -d -o root -g tiendanube -m 750 /etc/tiendanube
if [[ ! -f /etc/tiendanube/tiendanube.env ]]; then
    install -o root -g tiendanube -m 640 tiendanube.env.ejemplo /etc/tiendanube/tiendanube.env
    echo "   -> Completa /etc/tiendanube/tiendanube.env con la cadena de conexión de RDS y el admin semilla."
fi

echo "== Nginx"
install -m 644 nginx-tiendanube.conf /etc/nginx/conf.d/tiendanube.conf
nginx -t
systemctl enable --now nginx
systemctl reload nginx

echo "== Servicio systemd"
install -m 644 tiendanube.service /etc/systemd/system/tiendanube.service
systemctl daemon-reload
systemctl enable tiendanube

echo "Listo. Siguiente paso: aplicar migraciones (README, sección Despliegue) y publicar con publicar.ps1 o publicar.sh."
