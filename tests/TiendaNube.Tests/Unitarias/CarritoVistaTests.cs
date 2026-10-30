using TiendaNube.Web.Models.ViewModels;

namespace TiendaNube.Tests.Unitarias;

public class CarritoVistaTests
{
    private static LineaCarrito Linea(decimal precio, int cantidad, int stock = 100) =>
        new() { ProductoId = 1, Nombre = "Producto", PrecioUnitario = precio, Cantidad = cantidad, StockDisponible = stock };

    [Fact]
    public void Subtotal_EsPrecioPorCantidad()
    {
        Assert.Equal(1049.70m, Linea(349.90m, 3).Subtotal);
    }

    [Fact]
    public void Total_SumaSubtotalesSinErrorDeRedondeo()
    {
        // Con double, 0.1 * 3 + 349.9 * 3 ya no da un valor exacto; con decimal sí
        var vista = new CarritoVista { Lineas = [Linea(0.10m, 3), Linea(349.90m, 3), Linea(129.00m, 2)] };

        Assert.Equal(1308.00m, vista.Total);
        Assert.Equal(8, vista.TotalArticulos);
    }

    [Fact]
    public void CarritoVacio_TotalCeroYNoSePuedePagar()
    {
        var vista = new CarritoVista();

        Assert.True(vista.EstaVacio);
        Assert.Equal(0m, vista.Total);
        Assert.False(vista.PuedePagar);
    }

    [Fact]
    public void PuedePagar_FalsoSiUnaLineaExcedeElStock()
    {
        var vista = new CarritoVista { Lineas = [Linea(10m, 2, stock: 5), Linea(20m, 4, stock: 3)] };

        Assert.False(vista.Lineas[1].HayStockSuficiente);
        Assert.False(vista.PuedePagar);
    }

    [Fact]
    public void PuedePagar_VerdaderoSiCantidadIgualAlStock()
    {
        var vista = new CarritoVista { Lineas = [Linea(10m, 3, stock: 3)] };

        Assert.True(vista.PuedePagar);
    }
}
