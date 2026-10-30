using TiendaNube.Web.Models.ViewModels;

namespace TiendaNube.Web.Services;

public interface ICarritoService
{
    IReadOnlyList<ItemCarrito> ObtenerItems();

    int ContarArticulos();

    Task<ResultadoOperacion> AgregarAsync(int productoId, int cantidad);

    Task<ResultadoOperacion> ActualizarCantidadAsync(int productoId, int cantidad);

    void Quitar(int productoId);

    void Vaciar();

    Task<CarritoVista> ObtenerVistaAsync();
}
