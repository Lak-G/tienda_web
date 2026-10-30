using Microsoft.EntityFrameworkCore;
using Npgsql;
using TiendaNube.Tests.Apoyo;

namespace TiendaNube.Tests.BaseDatos;

// Las CHECK de la migración son la última defensa si un error en el código intentara guardar valores inválidos
[Collection(ColeccionBaseDatos.Nombre)]
public class RestriccionesTests(BaseDatosPruebas bd) : IAsyncLifetime
{
    public Task InitializeAsync() => bd.ReiniciarAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task StockNegativo_LoRechazaLaBaseDeDatos()
    {
        var gorra = (await bd.SembrarAsync(("Gorra", 179.50m, 1)))[0];
        await using var db = bd.CrearContexto();
        var producto = await db.Productos.SingleAsync(p => p.Id == gorra.Id);
        producto.Stock = -1;

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.Equal("CK_Productos_Stock", Assert.IsType<PostgresException>(error.InnerException).ConstraintName);
    }

    [Fact]
    public async Task PrecioCero_LoRechazaLaBaseDeDatos()
    {
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => bd.SembrarAsync(("Gratis", 0m, 1)));

        Assert.Equal("CK_Productos_Precio", Assert.IsType<PostgresException>(error.InnerException).ConstraintName);
    }
}
