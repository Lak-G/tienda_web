namespace TiendaNube.Web.Models.ViewModels;

public interface IPaginado
{
    int PaginaActual { get; }
    int TotalPaginas { get; }
    bool TieneAnterior { get; }
    bool TieneSiguiente { get; }
}

public class PaginaResultado<T> : IPaginado
{
    public IReadOnlyList<T> Elementos { get; init; } = [];
    public int PaginaActual { get; init; }
    public int TotalPaginas { get; init; }
    public int TotalElementos { get; init; }

    public bool TieneAnterior => PaginaActual > 1;
    public bool TieneSiguiente => PaginaActual < TotalPaginas;
}
