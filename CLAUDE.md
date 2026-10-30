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
| BD | PostgreSQL 16, instalado **en la misma instancia EC2**, escuchando solo en `localhost` |
| Imágenes | Amazon S3 vía `AWSSDK.S3`. Credenciales tomadas del instance profile (`LabInstanceProfile`), **nunca access keys en el código** |
| Auth admin | Cookie authentication simple (sin ASP.NET Identity). Contraseñas con `PasswordHasher<T>` |
| Carrito | Sesión del servidor (`ISession`, JSON serializado) |
| Frontend | HTML renderizado en servidor + CSS mínimo. Sin frameworks JS |
| Servidor | Kestrel como servicio `systemd`, detrás de Nginx (puerto 80) |
| Infra | EC2 t3.micro (1 GB RAM, Amazon Linux 2023) + bucket S3, en AWS Academy Learner Lab |

## Restricción principal: memoria

La instancia tiene 1 GB de RAM y comparte memoria entre la app y Postgres. Por eso:

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
- El bucket permite lectura pública solo en el prefijo `productos/`. La escritura va por el rol de la instancia.

## Configuración y secretos

- `appsettings.json` sin secretos. La cadena de conexión, el bucket y las credenciales del admin semilla vienen de **variables de entorno** (`ConnectionStrings__Default`, `S3__Bucket`, `S3__Region`, `AdminSemilla__Usuario`, `AdminSemilla__Contrasena`).
- En desarrollo: `dotnet user-secrets`.
- `.gitignore` estándar de .NET más `publish/` y `wwwroot/uploads/`.

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
├── tests/TiendaNube.Tests/   xUnit (carrito, totales, checkout con stock insuficiente)
└── deploy/
    ├── setup-ec2.sh           swap, Postgres, Nginx, usuario de servicio
    ├── postgresql-tuning.conf shared_buffers=64MB, work_mem=2MB, max_connections=20
    ├── nginx-tiendanube.conf
    ├── tiendanube.service     unidad systemd con variables de entorno
    └── publicar.sh            publish + scp + restart
```

## Despliegue (resumen)

1. EC2 t3.micro con Amazon Linux 2023, instance profile `LabInstanceProfile`, Elastic IP.
2. Security Group: 22 solo desde la IP del equipo, 80 abierto, **5432 cerrado**.
3. `deploy/setup-ec2.sh`: swap de 1 GB, `dnf install postgresql16-server nginx`, ajustes de Postgres, base de datos y usuario.
4. Migraciones: generar un script idempotente (`dotnet ef migrations script --idempotent`) y ejecutarlo con `psql` en la instancia.
5. `deploy/publicar.sh`: publish self-contained → `scp` → `systemctl restart tiendanube`.

## Forma de trabajo

- Trabajar por fases. Al terminar cada una: compilar, correr las pruebas y detenerse para revisión antes de seguir.
- No inventar requisitos fuera de la rúbrica sin preguntar.
- Si una decisión cambia el modelo de datos o el stack, preguntar primero.
- Commits pequeños con mensajes en español (`feat: carrito en sesión`, `fix: validar stock en checkout`).
