using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TiendaNube.Tests.Apoyo;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Tests.Unitarias;

public class CarritoSesionTests
{
    private const string ClaveSesion = "Carrito";

    private readonly SesionEnMemoria _sesion = new();
    private readonly CarritoService _carrito;

    public CarritoSesionTests()
    {
        _carrito = new CarritoService(ContextoSinConexion.Crear(), _sesion.ComoAccesor());
    }

    private void Guardar(params (int ProductoId, int Cantidad)[] items) =>
        _sesion.SetString(ClaveSesion, JsonSerializer.Serialize(
            items.Select(i => new ItemCarrito { ProductoId = i.ProductoId, Cantidad = i.Cantidad })));

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Agregar_CantidadNoPositiva_SeRechazaSinTocarLaSesion(int cantidad)
    {
        var resultado = await _carrito.AgregarAsync(1, cantidad);

        Assert.False(resultado.Exito);
        Assert.Equal("La cantidad debe ser mayor a 0.", resultado.Error);
        Assert.Empty(_carrito.ObtenerItems());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ActualizarCantidad_NoPositiva_SeRechazaYConservaLaAnterior(int cantidad)
    {
        Guardar((1, 2));

        var resultado = await _carrito.ActualizarCantidadAsync(1, cantidad);

        Assert.False(resultado.Exito);
        Assert.Equal(2, _carrito.ObtenerItems().Single().Cantidad);
    }

    [Fact]
    public void ContarArticulos_SumaLasCantidades()
    {
        Guardar((1, 2), (2, 5));

        Assert.Equal(7, _carrito.ContarArticulos());
    }

    [Fact]
    public void Quitar_EliminaSoloEseProducto()
    {
        Guardar((1, 2), (2, 5));

        _carrito.Quitar(1);

        var item = Assert.Single(_carrito.ObtenerItems());
        Assert.Equal(2, item.ProductoId);
    }

    [Fact]
    public void Quitar_ElUltimoProducto_BorraLaClaveDeSesion()
    {
        Guardar((1, 2));

        _carrito.Quitar(1);

        Assert.DoesNotContain(ClaveSesion, _sesion.Keys);
    }

    [Fact]
    public void Vaciar_DejaElCarritoSinArticulos()
    {
        Guardar((1, 2), (2, 5));

        _carrito.Vaciar();

        Assert.Empty(_carrito.ObtenerItems());
        Assert.Equal(0, _carrito.ContarArticulos());
    }

    [Fact]
    public void SesionCorrupta_SeTrataComoCarritoVacio()
    {
        _sesion.SetString(ClaveSesion, "{esto no es json");

        Assert.Empty(_carrito.ObtenerItems());
        Assert.DoesNotContain(ClaveSesion, _sesion.Keys);
    }
}
