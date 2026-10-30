using TiendaNube.Web.Models.ViewModels;

namespace TiendaNube.Web.Services;

public interface IOrdenService
{
    Task<ResultadoOrden> CrearOrdenAsync(DatosCliente datos, IReadOnlyList<ItemCarrito> items);

    Task<OrdenResumen?> ObtenerResumenAsync(int ordenId);

    Task<PaginaResultado<OrdenFila>> ListarAsync(int pagina, int tamanoPagina = 20);
}
