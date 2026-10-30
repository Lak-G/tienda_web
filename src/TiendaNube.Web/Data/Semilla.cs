using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaNube.Web.Models;

namespace TiendaNube.Web.Data;

public static class Semilla
{
    public static async Task EjecutarAsync(IServiceProvider servicios)
    {
        using var alcance = servicios.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuracion = alcance.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = alcance.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Semilla));

        await SembrarCatalogoAsync(db, logger);
        await SembrarAdministradorAsync(db, configuracion, logger);
    }

    private static async Task SembrarCatalogoAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Categorias.AnyAsync())
            return;

        var electronica = new Categoria { Nombre = "Electrónica" };
        var hogar = new Categoria { Nombre = "Hogar" };
        var ropa = new Categoria { Nombre = "Ropa" };
        var papeleria = new Categoria { Nombre = "Papelería" };

        db.Categorias.AddRange(electronica, hogar, ropa, papeleria);
        db.Productos.AddRange(
            new Producto { Nombre = "Audífonos inalámbricos", Descripcion = "Audífonos Bluetooth con estuche de carga y 20 horas de batería.", Precio = 899.00m, Stock = 15, Categoria = electronica },
            new Producto { Nombre = "Teclado mecánico", Descripcion = "Teclado compacto con switches rojos y retroiluminación.", Precio = 1249.50m, Stock = 8, Categoria = electronica },
            new Producto { Nombre = "Cargador USB-C 30 W", Descripcion = "Cargador de carga rápida compatible con teléfonos y tabletas.", Precio = 349.90m, Stock = 25, Categoria = electronica },
            new Producto { Nombre = "Taza de cerámica", Descripcion = "Taza de 350 ml apta para microondas y lavavajillas.", Precio = 129.00m, Stock = 40, Categoria = hogar },
            new Producto { Nombre = "Lámpara de escritorio LED", Descripcion = "Lámpara con brazo ajustable y tres tonos de luz.", Precio = 549.00m, Stock = 10, Categoria = hogar },
            new Producto { Nombre = "Juego de toallas", Descripcion = "Juego de tres toallas de algodón.", Precio = 459.00m, Stock = 0, Categoria = hogar },
            new Producto { Nombre = "Playera básica", Descripcion = "Playera de algodón, corte unisex.", Precio = 199.00m, Stock = 30, Categoria = ropa },
            new Producto { Nombre = "Sudadera con capucha", Descripcion = "Sudadera de felpa con bolsa frontal.", Precio = 649.00m, Stock = 12, Categoria = ropa },
            new Producto { Nombre = "Gorra ajustable", Descripcion = "Gorra de gabardina con cierre ajustable.", Precio = 179.50m, Stock = 3, Categoria = ropa },
            new Producto { Nombre = "Cuaderno profesional", Descripcion = "Cuaderno de 100 hojas, cuadro chico.", Precio = 65.00m, Stock = 50, Categoria = papeleria },
            new Producto { Nombre = "Set de plumas de gel", Descripcion = "Paquete de 10 plumas de gel de colores.", Precio = 119.00m, Stock = 20, Categoria = papeleria },
            new Producto { Nombre = "Mochila escolar", Descripcion = "Mochila con compartimento acolchado para laptop de 15\".", Precio = 799.00m, Stock = 6, Categoria = papeleria });

        await db.SaveChangesAsync();
        logger.LogInformation("Semilla: catálogo inicial creado.");
    }

    private static async Task SembrarAdministradorAsync(AppDbContext db, IConfiguration configuracion, ILogger logger)
    {
        if (await db.Administradores.AnyAsync())
            return;

        var usuario = configuracion["AdminSemilla:Usuario"];
        var contrasena = configuracion["AdminSemilla:Contrasena"];
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena))
        {
            logger.LogWarning("Semilla: no hay administradores y faltan AdminSemilla:Usuario/AdminSemilla:Contrasena; no se creó ninguno.");
            return;
        }

        var admin = new Administrador { Usuario = usuario };
        admin.HashContrasena = new PasswordHasher<Administrador>().HashPassword(admin, contrasena);
        db.Administradores.Add(admin);

        await db.SaveChangesAsync();
        logger.LogInformation("Semilla: administrador {Usuario} creado.", usuario);
    }
}
