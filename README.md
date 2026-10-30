# TiendaNube

Prototipo de tienda en línea para la materia **Cómputo en la Nube** (UABC): catálogo público con carrito y checkout, y panel de administración para productos, categorías, órdenes y stock. Desplegado en AWS (EC2 + RDS + S3).

## Stack

| Capa | Tecnología |
|---|---|
| Aplicación | ASP.NET Core 10 (LTS), Razor Pages, HTML renderizado en servidor y un solo `site.css` (sin frameworks JS) |
| Datos | EF Core 10 + Npgsql, PostgreSQL 16 con migraciones |
| Imágenes | Amazon S3 (`AWSSDK.S3`) en producción; `wwwroot/uploads` en desarrollo |
| Autenticación | Cookie del panel admin, contraseñas con `PasswordHasher<T>` |
| Carrito | Sesión del servidor (`ISession`), guarda solo id y cantidad |
| Pruebas | xUnit: unitarias puras y de integración contra PostgreSQL real |

## Arquitectura en AWS (us-east-2)

```
Navegador ──HTTP:80──► EC2 t3.micro (Amazon Linux 2023)
                        Nginx :80 ──► Kestrel 127.0.0.1:5000 (servicio systemd "tiendanube")
                          │                         │
                          │  rol IAM (TiendaNube-EC2-Role)
                          ▼                         ▼  TLS (SSL Mode=Require)
                 S3 tiendanube-bucket        RDS PostgreSQL 16 :5432
                 lectura pública solo        (solo accesible desde el
                 en productos/               security group del EC2)
```

- **EC2**: el Security Group abre el 22 solo desde la IP del equipo y el 80 a todo el mundo.
- **RDS**: su Security Group acepta el 5432 **solo** desde el Security Group del EC2. No es público.
- **S3**: la app escribe con el rol de la instancia. En el código no hay access keys.
- **Sitio**: http://3.146.91.228

## Desarrollo local

### Requisitos

- .NET SDK 10
- PostgreSQL 16 escuchando en `localhost`

### 1. Base de datos

Como superusuario (`postgres`):

```sql
CREATE ROLE tienda LOGIN PASSWORD 'una-contraseña-local';
CREATE DATABASE tiendanube OWNER tienda;
CREATE DATABASE tiendanube_test OWNER tienda;   -- la usan las pruebas de integración
```

### 2. Secretos de desarrollo

Nada sensible va en `appsettings.json`. En desarrollo se usan user-secrets:

```powershell
cd src\TiendaNube.Web
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=tiendanube;Username=tienda;Password=una-contraseña-local"
dotnet user-secrets set "AdminSemilla:Usuario" "admin"
dotnet user-secrets set "AdminSemilla:Contrasena" "una-contraseña-segura"
```

### 3. Migraciones y arranque

Desde la raíz del repositorio:

```powershell
dotnet tool restore                                   # instala dotnet-ef (manifiesto local)
dotnet ef database update --project src/TiendaNube.Web
dotnet run --project src/TiendaNube.Web
```

Al arrancar, la semilla crea 4 categorías, 12 productos y el administrador inicial, con las credenciales de `AdminSemilla`. Solo actúa si las tablas están vacías. El panel está en `/Admin`.

## Pruebas

```powershell
dotnet test
```

- **Unitarias** (`tests/TiendaNube.Tests/Unitarias`). No necesitan base de datos. Cubren:
  - Los totales del carrito con `decimal`.
  - Las operaciones del carrito sobre la sesión.
  - Las validaciones del checkout que ocurren antes de abrir la transacción.
- **Integración** (`tests/TiendaNube.Tests/BaseDatos`). Corren contra PostgreSQL real en `tiendanube_test`, sin InMemory ni Docker. En cada corrida se borra el esquema `public` y se recrea con las migraciones. Cubren:
  - El checkout completo: descuento de stock, totales exactos, precio congelado en el detalle y aborto total si un producto falla.
  - El carrito con precios leídos de la base de datos.
  - Las `CHECK` de la base de datos.
  - **Dos compras simultáneas del último artículo.** Las dos leen `Stock = 1`. El token `xmin` hace que la segunda falle con un conflicto, reintente y vea que el producto se agotó. Al final hay una sola orden y el stock queda en 0, no en negativo.

La cadena de conexión de pruebas se toma de la variable de entorno `TIENDANUBE_TEST_DB` o, si no existe, de los user-secrets del proyecto de pruebas:

```powershell
dotnet user-secrets set TIENDANUBE_TEST_DB "Host=localhost;Database=tiendanube_test;Username=tienda;Password=una-contraseña-local" --project tests/TiendaNube.Tests
```

> Como protección, las pruebas se niegan a correr si el nombre de la base de datos no termina en `_test`, porque cada corrida borra su esquema.

## Configuración

| Clave (variable de entorno) | Ejemplo | Notas |
|---|---|---|
| `ConnectionStrings__Default` | `Host=...rds.amazonaws.com;Database=tiendanube;Username=tienda;Password=...;SSL Mode=Require` | **Secreto**. En producción, siempre con `SSL Mode=Require` |
| `AdminSemilla__Usuario` / `AdminSemilla__Contrasena` | `admin` / `...` | **Secreto**. Solo se usa si no existe un administrador con ese usuario |
| `Almacenamiento__Proveedor` | `S3` | Con cualquier otro valor guarda en `wwwroot/uploads` |
| `S3__Bucket` / `S3__Region` | `tiendanube-bucket` / `us-east-2` | |
| `DataProtection__RutaClaves` | `/var/lib/tiendanube/claves` | Persiste las claves que cifran las cookies, para que un reinicio no cierre sesiones |
| `ZonaHoraria` | `America/Tijuana` | Zona en la que se muestran las fechas |

En el EC2, los valores no secretos están en `deploy/tiendanube.service`. Los secretos van en `/etc/tiendanube/tiendanube.env` (plantilla: `deploy/tiendanube.env.ejemplo`).

## Despliegue en AWS

La carpeta `deploy/` contiene:

| Archivo | Para qué |
|---|---|
| `setup-ec2.sh` | Documenta cómo se preparó el EC2: swap, Nginx, cliente `psql`, `libicu`, usuario de servicio, directorios y unidad systemd. Es idempotente |
| `nginx-tiendanube.conf` | Proxy inverso del puerto 80 a Kestrel, con `client_max_body_size 3M` |
| `tiendanube.service` | Unidad systemd con las variables de entorno |
| `tiendanube.env.ejemplo` | Plantilla de los secretos del servidor |
| `migraciones.sql` | Script idempotente de migraciones para RDS |
| `aplicar-migraciones.sh` | Corre en el EC2: aplica `migraciones.sql` con la cadena de conexión del `.env`, sin mostrar la contraseña |
| `publicar.ps1` / `publicar.sh` | Publish linux-x64 self-contained, `scp` y reinicio por `ssh` |
| `instalar-version.sh` | Corre en el EC2: instala la versión nueva, conserva la anterior y verifica que la app responda |
| `s3-politica-bucket.json` | Política del bucket: lectura pública solo en `productos/` |

### 1. RDS

Desde el EC2, conectado con el usuario maestro de RDS:

```bash
psql "host=<endpoint-rds> port=5432 dbname=postgres user=<usuario-maestro> sslmode=require"
```

```sql
CREATE ROLE tienda LOGIN;
\password tienda
CREATE DATABASE tiendanube OWNER tienda;
```

Si la base ya existía con otro dueño (por ejemplo, la creó la consola de RDS con el usuario maestro), `tienda` podrá conectarse pero no crear tablas. Desde PostgreSQL 15, solo el dueño de la base crea en `public`. En ese caso, conéctate a **`dbname=tiendanube`** con el usuario maestro y ejecuta:

```sql
GRANT ALL ON SCHEMA public TO tienda;
```

### 2. S3

1. En `tiendanube-bucket`, desactiva solo la opción de Block Public Access que bloquea las **políticas** públicas.
2. Pega la política de `deploy/s3-politica-bucket.json`.

Con esto, las imágenes de `productos/` son públicas y el resto del bucket no. La escritura la hace el rol de la instancia (`TiendaNube-EC2-Role`).

### 3. EC2

La instancia de producción (`3.146.91.228`) ya está preparada con este script. Su rol IAM es `TiendaNube-EC2-Role`, que solo tiene permiso para leer, subir y borrar objetos en el bucket. Para replicarla en otra instancia, copia `deploy/` y ejecuta `setup-ec2.sh`. El script solo agrega lo que falte, pero **reemplaza** `/etc/nginx/conf.d/tiendanube.conf`.

```bash
scp -i tiendanube.pem -r deploy ec2-user@<ip-ec2>:~/
ssh -i tiendanube.pem ec2-user@<ip-ec2> 'sudo bash ~/deploy/setup-ec2.sh'
```

Después, completa `/etc/tiendanube/tiendanube.env` con `sudo nano /etc/tiendanube/tiendanube.env`, usando la cadena de conexión de RDS y el admin semilla.

### 4. Migraciones (antes del primer arranque y cada vez que haya una migración nueva)

Cuando cambien las migraciones, genera de nuevo el script en tu máquina:

```powershell
dotnet ef migrations script --idempotent --project src/TiendaNube.Web --output deploy/migraciones.sql
```

Súbelo y aplícalo desde el EC2 hacia RDS. Como es idempotente, solo ejecuta las migraciones que falten. `aplicar-migraciones.sh` toma la cadena de conexión de `/etc/tiendanube/tiendanube.env`, así que la contraseña no se escribe en la terminal:

```bash
scp -i tiendanube.pem deploy/migraciones.sql deploy/aplicar-migraciones.sh ec2-user@<ip-ec2>:~/deploy/
ssh -i tiendanube.pem ec2-user@<ip-ec2> 'sudo bash ~/deploy/aplicar-migraciones.sh --probar'   # solo prueba la conexión
ssh -i tiendanube.pem ec2-user@<ip-ec2> 'sudo bash ~/deploy/aplicar-migraciones.sh'            # aplica migraciones.sql
```

También se puede usar `psql` directamente:

```bash
psql "host=<endpoint-rds> port=5432 dbname=tiendanube user=tienda sslmode=require" -v ON_ERROR_STOP=1 -f migraciones.sql
```

> Para validar también el certificado de RDS, descarga `https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem`. Después usa `sslmode=verify-full sslrootcert=global-bundle.pem` en psql, o `SSL Mode=VerifyFull;Root Certificate=/ruta/global-bundle.pem` en la cadena de la app.

### 5. Publicar

Desde Windows, con PowerShell en la raíz del repositorio:

```powershell
powershell -ExecutionPolicy Bypass -File deploy\publicar.ps1 -Servidor <ip-ec2> -Llave $HOME\.ssh\tiendanube.pem
```

Desde Linux, macOS o Git Bash:

```bash
deploy/publicar.sh <ip-ec2> ~/.ssh/tiendanube.pem
```

El script hace lo siguiente:
1. Compila un ejecutable self-contained para linux-x64. El EC2 no necesita SDK ni runtime de .NET.
2. Lo empaqueta en un `.tar.gz` y lo sube.
3. En el servidor, `instalar-version.sh` lo coloca en `/opt/tiendanube/app` y reinicia el servicio.
4. Espera hasta 30 s a que la app responda. Si no responde, muestra el log.

### Operación

```bash
sudo systemctl status tiendanube
sudo journalctl -u tiendanube -f            # log en vivo
```

Para regresar a la versión anterior:

```bash
sudo systemctl stop tiendanube
sudo mv /opt/tiendanube/app /opt/tiendanube/fallida && sudo mv /opt/tiendanube/anterior /opt/tiendanube/app
sudo systemctl start tiendanube
```

## Estructura

```
├── src/TiendaNube.Web/
│   ├── Data/          AppDbContext (restricciones con Fluent API), Semilla, Migrations/
│   ├── Models/        Entidades y ViewModels
│   ├── Services/      Carrito, Orden (checkout transaccional), Producto, Categoria, Autenticacion, Almacenamiento/
│   ├── Pages/         Catálogo, Detalle, Carrito, Checkout, Confirmacion, Admin/
│   └── wwwroot/css/site.css
├── tests/TiendaNube.Tests/   Unitarias/, BaseDatos/, Apoyo/
└── deploy/
```

## Modelo de datos

```
Categorias      (Id PK, Nombre UNIQUE)
Productos       (Id PK, Nombre, Descripcion, Precio numeric(10,2) CHECK > 0, Stock CHECK >= 0,
                 CategoriaId FK RESTRICT, ImagenUrl, Activo, FechaCreacion, xmin = token de concurrencia)
Clientes        (Id PK, Nombre, Correo, Telefono, Direccion)
Ordenes         (Id PK, ClienteId FK RESTRICT, Fecha, Total CHECK >= 0, Estado)
DetallesOrden   (Id PK, OrdenId FK CASCADE, ProductoId FK RESTRICT, Cantidad CHECK > 0,
                 PrecioUnitario CHECK > 0, Subtotal CHECK >= 0, UNIQUE(OrdenId, ProductoId))
Administradores (Id PK, Usuario UNIQUE, HashContrasena)
```

Un producto que ya tiene órdenes no se borra físicamente: se desactiva (`Activo = false`).
