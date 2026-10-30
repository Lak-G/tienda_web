using Microsoft.EntityFrameworkCore;
using TiendaNube.Web.Data;
using TiendaNube.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IProductoService, ProductoService>();
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

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
