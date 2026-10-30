using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TiendaNube.Web.Data;
using TiendaNube.Web.Models;

namespace TiendaNube.Tests.Apoyo;

[CollectionDefinition(Nombre)]
public class ColeccionBaseDatos : ICollectionFixture<BaseDatosPruebas>
{
    // Las pruebas de esta colección comparten la BD y por eso corren una tras otra
    public const string Nombre = "BaseDatos";
}

/// <summary>
/// PostgreSQL real (tiendanube_test). Al iniciar la corrida borra el esquema public y lo recrea
/// con las migraciones; cada prueba vacía las tablas con <see cref="ReiniciarAsync"/>.
/// </summary>
public sealed class BaseDatosPruebas : IAsyncLifetime
{
    public const string Variable = "TIENDANUBE_TEST_DB";

    public string CadenaConexion { get; }

    public BaseDatosPruebas()
    {
        // La variable de entorno tiene prioridad sobre los user-secrets
        var configuracion = new ConfigurationBuilder()
            .AddUserSecrets<BaseDatosPruebas>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        CadenaConexion = configuracion[Variable] ?? throw new InvalidOperationException(
            $"Falta la cadena de conexión de pruebas. Define la variable de entorno {Variable} o ejecuta: " +
            $"dotnet user-secrets set {Variable} \"Host=localhost;Database=tiendanube_test;Username=...;Password=...\" --project tests/TiendaNube.Tests");

        // Protección: InitializeAsync borra el esquema completo, nunca debe apuntar a la BD de desarrollo
        var baseDatos = new NpgsqlConnectionStringBuilder(CadenaConexion).Database;
        if (baseDatos is null || !baseDatos.EndsWith("_test", StringComparison.Ordinal))
            throw new InvalidOperationException($"{Variable} debe apuntar a una base de datos cuyo nombre termine en _test (es «{baseDatos}»).");
    }

    public async Task InitializeAsync()
    {
        await using var db = CrearContexto();
        await db.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public;");
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public AppDbContext CrearContexto(params IInterceptor[] interceptores) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(CadenaConexion)
            .AddInterceptors(interceptores)
            .Options);

    public async Task ReiniciarAsync()
    {
        await using var db = CrearContexto();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE \"DetallesOrden\", \"Ordenes\", \"Clientes\", \"Productos\", \"Categorias\", \"Administradores\" RESTART IDENTITY CASCADE;");
    }

    public async Task<Producto[]> SembrarAsync(params (string Nombre, decimal Precio, int Stock)[] productos)
    {
        await using var db = CrearContexto();
        var categoria = new Categoria { Nombre = "Pruebas" };
        var entidades = productos
            .Select(p => new Producto { Nombre = p.Nombre, Precio = p.Precio, Stock = p.Stock, Categoria = categoria })
            .ToArray();

        db.Productos.AddRange(entidades);
        await db.SaveChangesAsync();
        return entidades;
    }
}
