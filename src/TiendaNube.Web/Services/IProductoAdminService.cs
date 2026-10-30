using TiendaNube.Web.Models.ViewModels;

namespace TiendaNube.Web.Services;

public enum ResultadoEliminacion
{
    Eliminado,
    Desactivado,
    NoEncontrado
}

public interface IProductoAdminService
{
    Task<PaginaResultado<ProductoAdminFila>> ListarAsync(string? busqueda, int pagina, bool ordenarPorStock = false, int tamanoPagina = 15);

    Task<ProductoFormulario?> ObtenerParaEditarAsync(int id);

    Task<ResultadoOperacion> CrearAsync(ProductoFormulario datos, IFormFile? imagen);

    Task<ResultadoOperacion> ActualizarAsync(ProductoFormulario datos, IFormFile? imagen);

    Task<InfoEliminacionProducto?> ObtenerInfoEliminacionAsync(int id);

    /// <summary>Borra el producto; si tiene órdenes lo desactiva (borrado lógico).</summary>
    Task<ResultadoEliminacion> EliminarAsync(int id);
}
