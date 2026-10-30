using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TiendaNube.Tests.Apoyo;
using TiendaNube.Web.Models;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Tests.BaseDatos;

[Collection(ColeccionBaseDatos.Nombre)]
public class CheckoutTests(BaseDatosPruebas bd) : IAsyncLifetime
{
    public Task InitializeAsync() => bd.ReiniciarAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly DatosCliente Cliente = new() { Nombre = "Ana López", Correo = "ana@ejemplo.com", Telefono = "6641234567" };

    private static ItemCarrito Item(Producto producto, int cantidad) => new() { ProductoId = producto.Id, Cantidad = cantidad };

    private async Task<ResultadoOrden> ComprarAsync(params ItemCarrito[] items)
    {
        await using var db = bd.CrearContexto();
        return await new OrdenService(db, NullLogger<OrdenService>.Instance).CrearOrdenAsync(Cliente, items);
    }

    private async Task<int> StockAsync(Producto producto)
    {
        await using var db = bd.CrearContexto();
        return await db.Productos.Where(p => p.Id == producto.Id).Select(p => p.Stock).SingleAsync();
    }

    private async Task<int> ContarOrdenesAsync()
    {
        await using var db = bd.CrearContexto();
        return await db.Ordenes.CountAsync();
    }

    [Fact]
    public async Task CompraValida_DescuentaStockYGuardaTotalesExactos()
    {
        var productos = await bd.SembrarAsync(("Cargador", 349.90m, 25), ("Taza", 129.00m, 40));
        var (cargador, taza) = (productos[0], productos[1]);

        var resultado = await ComprarAsync(Item(cargador, 3), Item(taza, 2));

        Assert.True(resultado.Exito, resultado.Error);
        Assert.Equal(22, await StockAsync(cargador));
        Assert.Equal(38, await StockAsync(taza));

        await using var db = bd.CrearContexto();
        var orden = await db.Ordenes.Include(o => o.Detalles).Include(o => o.Cliente).SingleAsync(o => o.Id == resultado.OrdenId);
        Assert.Equal(1307.70m, orden.Total);
        Assert.Equal(EstadoOrden.Pendiente, orden.Estado);
        Assert.Equal("ana@ejemplo.com", orden.Cliente!.Correo);
        Assert.Collection(orden.Detalles.OrderBy(d => d.ProductoId),
            d => { Assert.Equal(3, d.Cantidad); Assert.Equal(349.90m, d.PrecioUnitario); Assert.Equal(1049.70m, d.Subtotal); },
            d => { Assert.Equal(2, d.Cantidad); Assert.Equal(129.00m, d.PrecioUnitario); Assert.Equal(258.00m, d.Subtotal); });
    }

    [Fact]
    public async Task ProductoRepetidoEnElCarrito_SeAgrupaEnUnSoloDetalle()
    {
        var gorra = (await bd.SembrarAsync(("Gorra", 179.50m, 10)))[0];

        var resultado = await ComprarAsync(Item(gorra, 2), Item(gorra, 3));

        Assert.True(resultado.Exito, resultado.Error);
        Assert.Equal(5, await StockAsync(gorra));
        await using var db = bd.CrearContexto();
        var detalle = await db.DetallesOrden.SingleAsync();
        Assert.Equal(5, detalle.Cantidad);
        Assert.Equal(897.50m, detalle.Subtotal);
    }

    [Fact]
    public async Task StockInsuficiente_FallaConElNombreDelProductoYNoGuardaNada()
    {
        var gorra = (await bd.SembrarAsync(("Gorra ajustable", 179.50m, 3)))[0];

        var resultado = await ComprarAsync(Item(gorra, 5));

        Assert.False(resultado.Exito);
        Assert.Equal("No hay stock suficiente de «Gorra ajustable»: pediste 5 y quedan 3.", resultado.Error);
        Assert.Equal(3, await StockAsync(gorra));
        Assert.Equal(0, await ContarOrdenesAsync());
    }

    [Fact]
    public async Task UnProductoSinStock_AbortaLaOrdenCompleta()
    {
        var productos = await bd.SembrarAsync(("Taza", 129.00m, 40), ("Toallas", 459.00m, 0));

        var resultado = await ComprarAsync(Item(productos[0], 2), Item(productos[1], 1));

        Assert.False(resultado.Exito);
        Assert.Equal("«Toallas» se agotó. Quítalo del carrito para continuar.", resultado.Error);
        // La taza, que sí tenía stock, tampoco se descuenta: la transacción no se confirma
        Assert.Equal(40, await StockAsync(productos[0]));
        Assert.Equal(0, await ContarOrdenesAsync());
    }

    [Fact]
    public async Task ProductoDesactivado_NoSePuedeComprar()
    {
        var lampara = (await bd.SembrarAsync(("Lámpara", 549.00m, 10)))[0];
        await using (var db = bd.CrearContexto())
            await db.Productos.Where(p => p.Id == lampara.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Activo, false));

        var resultado = await ComprarAsync(Item(lampara, 1));

        Assert.False(resultado.Exito);
        Assert.Equal("«Lámpara» ya no está disponible. Quítalo del carrito para continuar.", resultado.Error);
        Assert.Equal(10, await StockAsync(lampara));
    }

    [Fact]
    public async Task ProductoInexistente_NoSePuedeComprar()
    {
        var resultado = await ComprarAsync(new ItemCarrito { ProductoId = 9999, Cantidad = 1 });

        Assert.False(resultado.Exito);
        Assert.Equal("Un producto de tu carrito ya no existe. Revisa el carrito.", resultado.Error);
    }

    [Fact]
    public async Task CambioDePrecioPosterior_NoAlteraElDetalleNiElTotal()
    {
        var teclado = (await bd.SembrarAsync(("Teclado", 1249.50m, 8)))[0];
        var resultado = await ComprarAsync(Item(teclado, 2));

        await using var db = bd.CrearContexto();
        await db.Productos.Where(p => p.Id == teclado.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Precio, 1500.00m));

        var orden = await db.Ordenes.AsNoTracking().Include(o => o.Detalles).SingleAsync(o => o.Id == resultado.OrdenId);
        Assert.Equal(2499.00m, orden.Total);
        Assert.Equal(1249.50m, orden.Detalles.Single().PrecioUnitario);
    }

    [Fact]
    public async Task DosComprasSimultaneasDelUltimoArticulo_SoloUnaGanaYElStockNoQuedaNegativo()
    {
        var gorra = (await bd.SembrarAsync(("Gorra", 179.50m, 1)))[0];

        // Ambas compras leen Stock = 1 y se detienen antes de guardar; luego se sueltan a la vez.
        // La segunda en escribir ejecuta UPDATE ... WHERE xmin = <el que leyó>, que ya no coincide,
        // así que afecta 0 filas, EF lanza DbUpdateConcurrencyException y el servicio reintenta
        var punto = new PuntoDeEncuentro(2);
        var interceptores = new[] { new EsperaAntesDeGuardar(punto), new EsperaAntesDeGuardar(punto) };

        var compras = interceptores.Select(async interceptor =>
        {
            await using var db = bd.CrearContexto(interceptor);
            return await new OrdenService(db, NullLogger<OrdenService>.Instance).CrearOrdenAsync(Cliente, [Item(gorra, 1)]);
        });
        var resultados = await Task.WhenAll(compras);

        Assert.Single(resultados, r => r.Exito);
        var perdedora = Assert.Single(resultados, r => !r.Exito);
        Assert.Equal("«Gorra» se agotó. Quítalo del carrito para continuar.", perdedora.Error);

        // Exactamente un conflicto detectado por xmin: prueba que el reintento lo provocó el token, no la validación
        Assert.Equal(1, interceptores.Sum(i => i.Conflictos));
        Assert.Equal(0, await StockAsync(gorra));
        Assert.Equal(1, await ContarOrdenesAsync());
    }
}
