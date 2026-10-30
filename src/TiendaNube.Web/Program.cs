using Microsoft.EntityFrameworkCore;
using TiendaNube.Web.Data;
using TiendaNube.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICarritoService, CarritoService>();
builder.Services.AddScoped<IOrdenService, OrdenService>();
builder.Services.AddHttpContextAccessor();

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

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
