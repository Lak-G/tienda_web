using Amazon;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using TiendaNube.Web.Data;
using TiendaNube.Web.Services;
using TiendaNube.Web.Services.Almacenamiento;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(opciones =>
{
    opciones.Conventions.AuthorizeFolder("/Admin");
    opciones.Conventions.AllowAnonymousToPage("/Admin/Login");
});
builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICarritoService, CarritoService>();
builder.Services.AddScoped<IOrdenService, OrdenService>();
builder.Services.AddScoped<IProductoAdminService, ProductoAdminService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();
builder.Services.AddSingleton<HoraLocal>();
builder.Services.AddHttpContextAccessor();

// Almacenamiento:Proveedor = "S3" en producción; cualquier otro valor guarda en wwwroot/uploads
if (string.Equals(builder.Configuration["Almacenamiento:Proveedor"], "S3", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IAmazonS3>(_ =>
        new AmazonS3Client(RegionEndpoint.GetBySystemName(builder.Configuration["S3:Region"])));
    builder.Services.AddSingleton<IAlmacenamientoImagenes, S3AlmacenamientoImagenes>();
}
else
{
    builder.Services.AddSingleton<IAlmacenamientoImagenes, LocalAlmacenamientoImagenes>();
}

// Imagen de hasta 2 MB más el resto del formulario
builder.Services.Configure<FormOptions>(opciones => opciones.MultipartBodyLengthLimit = 3 * 1024 * 1024);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Admin/Login";
        opciones.LogoutPath = "/Admin/Logout";
        opciones.AccessDeniedPath = "/Admin/Login";
        opciones.Cookie.Name = ".TiendaNube.Admin";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SameSite = SameSiteMode.Strict;
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

// Sesión en memoria del proceso: suficiente para una sola instancia; se pierde al reiniciar el servicio
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opciones =>
{
    opciones.IdleTimeout = TimeSpan.FromMinutes(30);
    opciones.Cookie.Name = ".TiendaNube.Sesion";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
    opciones.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddRequestLocalization(opciones =>
{
    opciones.SetDefaultCulture("es-MX");
    opciones.AddSupportedCultures("es-MX");
    opciones.AddSupportedUICultures("es-MX");
});

var app = builder.Build();

await Semilla.EjecutarAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRequestLocalization();

// MapStaticAssets solo sirve lo que existe al compilar; las imágenes subidas en ejecución van por aquí
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
