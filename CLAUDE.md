# TiendaNube — Contexto del proyecto

Proyecto escolar de la materia **Cómputo en la Nube** (UABC). Prototipo funcional de una tienda en línea con vista pública para el cliente y panel de administración, desplegado en AWS. Equipo de 3 personas, plazo de 1 semana.

## Rúbrica (lo que se evalúa)

- **Base de datos (25%)**: modelo normalizado, PK/FK, tipos correctos, restricciones de integridad, conexión segura. Nada de datos hardcodeados.
- **Backend y lógica (25–35%)**: CRUD de productos sin errores, carrito con totales exactos, validar y descontar stock al comprar, validaciones del lado del servidor y manejo de errores. No permitir totales ni stock negativos.
- **Frontend/UX (20%)**: limpio, intuitivo y responsivo (móvil y escritorio). Navegación clara entre catálogo, carrito y panel admin.
- **Integración, pruebas y despliegue (10%)**: desplegado en AWS y funcionando en la demo en vivo.
- **Código y documentación (10%)**: estructura modular, nombres descriptivos, README.md con instrucciones de instalación y configuración de la BD.

## Stack

| Capa | Tecnología |
|---|---|
| Framework | ASP.NET Core **10 (LTS)** con Razor Pages (.NET 8 deja de tener soporte en noviembre de 2026) |
| ORM | EF Core + Npgsql, con migraciones |
| BD | **Amazon RDS PostgreSQL 16** (us-east-2), no pública: el 5432 solo acepta conexiones del Security Group del EC2. Conexión con `SSL Mode=Require` |
| Imágenes | Amazon S3 vía `AWSSDK.S3`, bucket `tiendanube-bucket` (us-east-2). Credenciales tomadas del rol de la instancia (`TiendaNube-EC2-Role`), **nunca access keys en el código** |
| Auth admin | Cookie authentication simple (sin ASP.NET Identity). Contraseñas con `PasswordHasher<T>` |
| Carrito | Sesión del servidor (`ISession`, JSON serializado) |
| Frontend | HTML renderizado en servidor + CSS mínimo. Sin frameworks JS |
| Servidor | Kestrel como servicio `systemd`, detrás de Nginx (puerto 80) |
| Infra | EC2 t3.micro (1 GB RAM, Amazon Linux 2023, ya creado y configurado) + RDS PostgreSQL + bucket S3, en AWS Academy Learner Lab, región us-east-2 |

## Restricción principal: memoria

La instancia tiene 1 GB de RAM. La BD ya no vive en el EC2 (está en RDS), pero la app sigue compartiendo la máquina con Nginx y el sistema. Por eso:

- Sin Docker en el servidor.
- En el `.csproj`: `<ServerGarbageCollection>false</ServerGarbageCollection>` y `<ConcurrentGarbageCollection>false</ConcurrentGarbageCollection>`.
- Publicar **fuera** del EC2: `dotnet publish -c Release -r linux-x64 --self-contained` y subir con `scp` (así no hace falta instalar el SDK ni el runtime en la instancia).
- Consultas con `AsNoTracking()` en lecturas y paginación en el catálogo y el panel admin.
- No cargar dependencias pesadas. Antes de agregar un paquete NuGet, justificarlo.

## Convenciones de código

- **Idioma**: nombres de dominio, clases, propiedades, tablas y textos de UI en **español** (`Producto`, `Categoria`, `Orden`, `DetalleOrden`, `CarritoService`). Palabras reservadas y sufijos técnicos en inglés se aceptan (`Service`, `DbContext`, `PageModel`).
- C#: PascalCase para tipos y miembros públicos, `_camelCase` para campos privados, `async`/`await` en todo acceso a datos, nullable reference types activado.
- Lógica de negocio en `Services/`, no en los `PageModel`. Los `PageModel` solo orquestan.
- Inyección de dependencias por interfaz (`ICarritoService`, `IOrdenService`, `IAlmacenamientoImagenes`).
- Comentarios solo donde la lógica no sea obvia (transacciones, concurrencia).

### HTML y CSS

- **CSS mínimo**, un solo archivo `wwwroot/css/site.css`. Sin Bootstrap ni Tailwind.
- Clases e IDs con **nombres en español abreviados y en PascalCase con guion bajo**: `Encabezado`, `Btn_Princ`, `Btn_Sec`, `Tarj_Prod`, `Cont_Catalogo`, `Form_Admin`.
- Formato de reglas **sin espacio antes de la llave**:
  ```css
  .Tarj_Prod{
      padding: 1rem;
  }
  ```
- Variables CSS en `:root` para colores y espaciados.
- Responsivo con CSS Grid / Flexbox y media queries (mobile-first).
- **Sin JavaScript innecesario**. Todo el flujo (carrito, checkout, CRUD) funciona con formularios y POST. JS solo si es indispensable, y nunca para lógica de negocio.

## Modelo de datos

```
Categorias      (Id PK, Nombre UNIQUE NOT NULL)
Productos       (Id PK, Nombre NOT NULL, Descripcion, Precio numeric(10,2) CHECK > 0,
                 Stock int CHECK >= 0, CategoriaId FK -> Categorias, ImagenUrl,
                 Activo bool DEFAULT true, FechaCreacion timestamptz,
                 xmin como token de concurrencia)
Clientes        (Id PK, Nombre NOT NULL, Correo NOT NULL, Telefono, Direccion)
Ordenes         (Id PK, ClienteId FK -> Clientes, Fecha timestamptz, Total numeric(10,2) CHECK >= 0,
                 Estado varchar)
DetallesOrden   (Id PK, OrdenId FK -> Ordenes ON DELETE CASCADE, ProductoId FK -> Productos,
                 Cantidad int CHECK > 0, PrecioUnitario numeric(10,2), Subtotal numeric(10,2),
                 UNIQUE(OrdenId, ProductoId))
Administradores (Id PK, Usuario UNIQUE NOT NULL, HashContrasena NOT NULL)
```

- Restricciones definidas con Fluent API en `AppDbContext` (incluidas las `CHECK` vía `HasCheckConstraint`), para que queden en las migraciones.
- `DetallesOrden.PrecioUnitario` guarda el precio **al momento de la compra** (no se recalcula si cambia el producto).
- Borrar un producto con órdenes asociadas: FK `Restrict` y **borrado lógico** (`Activo = false`). Sin órdenes, borrado físico.
- Datos semilla: categorías, ~12 productos y un admin inicial, cuyas credenciales vienen de la configuración, no del código.

## Lógica de negocio crítica

### Carrito (sesión)

- Guarda solo `ProductoId` y `Cantidad`. **Precios y stock siempre se leen de la BD**, nunca de la sesión ni del formulario.
- Agregar, quitar, modificar cantidad y vaciar. La cantidad debe ser > 0 y ≤ stock disponible.
- El total se calcula en el servidor con `decimal`.

### Checkout (`OrdenService.CrearOrdenAsync`)

Todo dentro de **una transacción**:

1. Validar los datos del cliente (DataAnnotations y `ModelState`).
2. Releer los productos del carrito desde la BD.
3. Validar que cada producto siga activo y que `Stock >= Cantidad`. Si no, abortar con un mensaje claro de qué producto falló.
4. Descontar el stock, crear `Cliente`, `Orden` y `DetallesOrden`, calcular `Total`.
5. `SaveChanges` + `Commit`. Ante `DbUpdateConcurrencyException` (dos compras simultáneas), reintentar hasta 3 veces releyendo stock y, si persiste, informar al usuario.
6. Vaciar el carrito y redirigir a la página de confirmación con el número de orden.

### Panel admin

- Protegido con `[Authorize]` en la carpeta `/Admin` (convención de Razor Pages), con login y logout.
- CRUD de productos con validación de servidor. La subida de imagen va a S3 (validar tipo `image/*` y tamaño ≤ 2 MB). También se acepta una URL directa.
- Vista de órdenes con su detalle y vista de stock actual.
- Anti-forgery activado (por defecto en Razor Pages).

## Almacenamiento de imágenes

- Interfaz `IAlmacenamientoImagenes` con dos implementaciones:
  - `S3AlmacenamientoImagenes` (producción): sube a `s3://{Bucket}/productos/{guid}.{ext}` y devuelve la URL pública.
  - `LocalAlmacenamientoImagenes` (desarrollo): guarda en `wwwroot/uploads`.
- Se elige según `Almacenamiento:Proveedor` en la configuración.
- El bucket `tiendanube-bucket` (us-east-2) permite lectura pública solo en el prefijo `productos/` (política en `deploy/s3-politica-bucket.json`). La escritura va por el rol de la instancia.

## Configuración y secretos

- `appsettings.json` sin secretos. La cadena de conexión, el bucket y las credenciales del admin semilla vienen de **variables de entorno** (`ConnectionStrings__Default`, `S3__Bucket`, `S3__Region`, `AdminSemilla__Usuario`, `AdminSemilla__Contrasena`).
- En producción la cadena de conexión apunta a RDS y lleva `SSL Mode=Require`.
- En el EC2: los valores no secretos (`Almacenamiento__Proveedor=S3`, `S3__Bucket=tiendanube-bucket`, `S3__Region=us-east-2`, `DataProtection__RutaClaves`) van en `deploy/tiendanube.service`; los secretos en `/etc/tiendanube/tiendanube.env` (root:tiendanube, 640; plantilla en `deploy/tiendanube.env.ejemplo`).
- `DataProtection__RutaClaves` persiste las claves de Data Protection en disco para que un reinicio del servicio no invalide las cookies de admin, sesión y antiforgery.
- En desarrollo: `dotnet user-secrets`.
- Pruebas: la cadena de `tiendanube_test` viene de la variable `TIENDANUBE_TEST_DB` o, si no existe, de los user-secrets del proyecto de pruebas.
- `.gitignore` estándar de .NET más `publish/`, `wwwroot/uploads/` y `deploy/*.env`. `.gitattributes` fuerza LF en `*.sh`, `*.service`, `*.conf` y en `deploy/`.

## Estructura del proyecto

```
TiendaNube/
├── CLAUDE.md
├── README.md
├── TiendaNube.sln
├── src/TiendaNube.Web/
│   ├── Data/            AppDbContext.cs, Semilla.cs, Migrations/
│   ├── Models/          Entidades y ViewModels
│   ├── Services/        CarritoService, OrdenService, ProductoService, almacenamiento
│   ├── Pages/
│   │   ├── Index         (catálogo: filtro por categoría + búsqueda + paginación)
│   │   ├── Productos/Detalle
│   │   ├── Carrito
│   │   ├── Checkout, Confirmacion
│   │   └── Admin/       Login, Logout, Productos/{Index,Crear,Editar,Eliminar},
│   │                     Categorias, Ordenes/{Index,Detalle}
│   ├── wwwroot/css/site.css
│   └── Program.cs
├── tests/TiendaNube.Tests/   xUnit
│   ├── Unitarias/             sin BD: totales, carrito en sesión, validaciones previas del checkout
│   ├── BaseDatos/             PostgreSQL real (tiendanube_test): checkout, stock, compras simultáneas (xmin), CHECKs
│   └── Apoyo/                 fixture de BD (recrea el esquema con migraciones), sesión en memoria, punto de encuentro
└── deploy/
    ├── setup-ec2.sh           documentación para replicar el EC2: swap, Nginx, cliente psql, libicu, usuario de servicio, systemd
    ├── nginx-tiendanube.conf  proxy :80 → 127.0.0.1:5000, client_max_body_size 3M
    ├── tiendanube.service     unidad systemd con variables de entorno no secretas + EnvironmentFile
    ├── tiendanube.env.ejemplo plantilla de /etc/tiendanube/tiendanube.env (secretos)
    ├── migraciones.sql        script idempotente de migraciones para RDS
    ├── publicar.ps1           (Windows) publish linux-x64 self-contained + scp + reinicio por ssh
    ├── publicar.sh            lo mismo desde Linux/macOS/Git Bash
    ├── instalar-version.sh    corre en el EC2: instala la versión, conserva la anterior, verifica que responda
    ├── aplicar-migraciones.sh corre en el EC2: aplica migraciones.sql con la cadena del .env
    └── s3-politica-bucket.json lectura pública solo en productos/
```

## Pruebas

- Las pruebas de checkout corren contra **PostgreSQL real** (`tiendanube_test`, mismo usuario `tienda`), nunca InMemory, porque InMemory no soporta transacciones ni `xmin`. Sin Docker ni Testcontainers.
- Cada corrida borra el esquema `public` y lo recrea con las migraciones; cada prueba vacía las tablas. El fixture se niega a correr si la BD no termina en `_test`.
- Lo que no necesita BD (totales, carrito en sesión, validaciones previas) va como prueba unitaria pura.

## Despliegue (resumen)

1. **EC2** (`3.146.91.228`, ya preparado con `setup-ec2.sh`): t3.micro, Amazon Linux 2023, swap, Nginx, rol IAM `TiendaNube-EC2-Role` (solo objetos del bucket; no puede leer la configuración del bucket ni de RDS). `deploy/setup-ec2.sh` documenta cómo replicarlo; no instala Postgres, solo el cliente `psql` para las migraciones.
2. **Security Groups**: EC2 con 22 solo desde la IP del equipo y 80 abierto. RDS con 5432 solo desde el Security Group del EC2.
3. **RDS PostgreSQL** (us-east-2, `tiendanube-db`): BD `tiendanube`, cuyo dueño es el usuario maestro; `tienda` tiene `GRANT ALL ON SCHEMA public`. Conexión con `SSL Mode=Require`.
4. **S3**: `tiendanube-bucket` con la política de `deploy/s3-politica-bucket.json`.
5. **Migraciones**: `dotnet ef migrations script --idempotent -o deploy/migraciones.sql`, se sube al EC2 y se aplica con `sudo bash ~/deploy/aplicar-migraciones.sh`, que lee la cadena del `.env` sin mostrar la contraseña. Antes del primer arranque, porque la semilla corre al iniciar.
6. **Publicar**: `deploy/publicar.ps1` (Windows) o `deploy/publicar.sh` → publish self-contained linux-x64 → `.tar.gz` por `scp` → `instalar-version.sh` reinicia `tiendanube`.

## Forma de trabajo

- Trabajar por fases. Al terminar cada una: compilar, correr las pruebas y detenerse para revisión antes de seguir.
- No inventar requisitos fuera de la rúbrica sin preguntar.
- Si una decisión cambia el modelo de datos o el stack, preguntar primero.
- Commits pequeños con mensajes en español (`feat: carrito en sesión`, `fix: validar stock en checkout`).
