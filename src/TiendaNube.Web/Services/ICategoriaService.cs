using TiendaNube.Web.Models.ViewModels;

namespace TiendaNube.Web.Services;

public interface ICategoriaService
{
    Task<IReadOnlyList<CategoriaFila>> ListarAsync();

    Task<ResultadoOperacion> CrearAsync(string? nombre);

    Task<ResultadoOperacion> RenombrarAsync(int id, string? nombre);

    Task<ResultadoOperacion> EliminarAsync(int id);
}
