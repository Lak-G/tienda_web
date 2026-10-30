using Microsoft.EntityFrameworkCore;
using TiendaNube.Tests.Apoyo;
using TiendaNube.Web.Data;
using TiendaNube.Web.Services;

namespace TiendaNube.Tests.BaseDatos;

[Collection(ColeccionBaseDatos.Nombre)]
public class CarritoBaseDatosTests(BaseDatosPruebas bd) : IAsyncLifetime
{
    private readonly SesionEnMemoria _sesion = new();
    private AppDbContext _db = null!;
    private CarritoService _carrito = null!;

    public async Task InitializeAsync()
    {
        await bd.ReiniciarAsync();
        _db = bd.CrearContexto();
        _carrito = new CarritoService(_db, _sesion.ComoAccesor());
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Agregar_DosVeces_AcumulaLaCantidad()
    {
        var taza = (await bd.SembrarAsync(("Taza", 129.00m, 40)))[0];

        Assert.True((await _carrito.AgregarAsync(taza.Id, 2)).Exito);
        Assert.True((await _carrito.AgregarAsync(taza.Id, 3)).Exito);

        Assert.Equal(5, Assert.Single(_carrito.ObtenerItems()).Cantidad);
    }

    [Fact]
    public async Task Agregar_MasQueElStockContandoLoQueYaEstaEnElCarrito_SeRechaza()
    {
        var gorra = (await bd.SembrarAsync(("Gorra", 179.50m, 3)))[0];
        await _carrito.AgregarAsync(gorra.Id, 2);

        var resultado = await _carrito.AgregarAsync(gorra.Id, 2);

        Assert.False(resultado.Exito);
        Assert.Contains("puedes agregar 1 más", resultado.Error);
        Assert.Equal(2, _carrito.ContarArticulos());
    }

    [Fact]
    public async Task Agregar_ProductoAgotado_SeRechaza()
    {
        var toallas = (await bd.SembrarAsync(("Toallas", 459.00m, 0)))[0];

        var resultado = await _carrito.AgregarAsync(toallas.Id, 1);

        Assert.False(resultado.Exito);
        Assert.Equal("«Toallas» está agotado.", resultado.Error);
    }

    [Fact]
    public async Task ActualizarCantidad_PorEncimaDelStock_SeRechaza()
    {
        var gorra = (await bd.SembrarAsync(("Gorra", 179.50m, 3)))[0];
        await _carrito.AgregarAsync(gorra.Id, 1);

        var resultado = await _carrito.ActualizarCantidadAsync(gorra.Id, 4);

        Assert.False(resultado.Exito);
        Assert.Equal(1, _carrito.ContarArticulos());
    }

    [Fact]
    public async Task Vista_UsaElPrecioActualDeLaBdYCalculaElTotal()
    {
        var productos = await bd.SembrarAsync(("Cargador", 349.90m, 25), ("Taza", 129.00m, 40));
        await _carrito.AgregarAsync(productos[0].Id, 3);
        await _carrito.AgregarAsync(productos[1].Id, 2);

        // El precio cambia después de agregar: la sesión solo guarda id y cantidad, así que se usa el nuevo
        await _db.Productos.Where(p => p.Id == productos[1].Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Precio, 150.00m));

        var vista = await _carrito.ObtenerVistaAsync();

        Assert.Equal(150.00m, vista.Lineas.Single(l => l.ProductoId == productos[1].Id).PrecioUnitario);
        Assert.Equal(1349.70m, vista.Total);
        Assert.True(vista.PuedePagar);
    }

    [Fact]
    public async Task Vista_QuitaProductosDesactivadosYAvisa()
    {
        var productos = await bd.SembrarAsync(("Cargador", 349.90m, 25), ("Lámpara", 549.00m, 10));
        await _carrito.AgregarAsync(productos[0].Id, 1);
        await _carrito.AgregarAsync(productos[1].Id, 1);
        await _db.Productos.Where(p => p.Id == productos[1].Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Activo, false));

        var vista = await _carrito.ObtenerVistaAsync();

        Assert.Single(vista.Lineas);
        Assert.Single(vista.Avisos);
        Assert.Equal(349.90m, vista.Total);
        Assert.Equal(productos[0].Id, Assert.Single(_carrito.ObtenerItems()).ProductoId);
    }

    [Fact]
    public async Task Vista_StockQueBajoDespuesDeAgregar_ImpidePagar()
    {
        var gorra = (await bd.SembrarAsync(("Gorra", 179.50m, 3)))[0];
        await _carrito.AgregarAsync(gorra.Id, 3);
        await _db.Productos.Where(p => p.Id == gorra.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, 1));

        var vista = await _carrito.ObtenerVistaAsync();

        Assert.False(vista.PuedePagar);
    }
}
