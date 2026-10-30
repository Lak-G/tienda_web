using Microsoft.Extensions.Logging.Abstractions;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Tests.Unitarias;

// Validaciones que CrearOrdenAsync hace antes de abrir la transacción
public class OrdenValidacionTests
{
    private readonly OrdenService _ordenes = new(ContextoSinConexion.Crear(), NullLogger<OrdenService>.Instance);

    private static DatosCliente ClienteValido() => new() { Nombre = "Ana López", Correo = "ana@ejemplo.com" };

    [Fact]
    public async Task CarritoVacio_SeRechaza()
    {
        var resultado = await _ordenes.CrearOrdenAsync(ClienteValido(), []);

        Assert.False(resultado.Exito);
        Assert.Equal("El carrito está vacío.", resultado.Error);
    }

    [Fact]
    public async Task CantidadCero_SeRechaza()
    {
        var resultado = await _ordenes.CrearOrdenAsync(ClienteValido(), [new ItemCarrito { ProductoId = 1, Cantidad = 0 }]);

        Assert.False(resultado.Exito);
        Assert.Equal("Todas las cantidades deben ser mayores a 0.", resultado.Error);
    }

    [Theory]
    [InlineData("", "ana@ejemplo.com", "El nombre es obligatorio.")]
    [InlineData("Ana López", "", "El correo es obligatorio.")]
    [InlineData("Ana López", "no-es-correo", "El correo no es válido.")]
    public async Task DatosDelClienteInvalidos_SeRechazanConMensajeClaro(string nombre, string correo, string mensaje)
    {
        var datos = new DatosCliente { Nombre = nombre, Correo = correo };

        var resultado = await _ordenes.CrearOrdenAsync(datos, [new ItemCarrito { ProductoId = 1, Cantidad = 1 }]);

        Assert.False(resultado.Exito);
        Assert.Equal(mensaje, resultado.Error);
        Assert.Null(resultado.OrdenId);
    }
}
