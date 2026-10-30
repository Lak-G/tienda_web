using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TiendaNube.Web.Models;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages;

public class IndexModel(IProductoService productoService) : PageModel
{
    private readonly IProductoService _productoService = productoService;

    [BindProperty(SupportsGet = true)]
    public int? Categoria { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    public IReadOnlyList<Categoria> Categorias { get; private set; } = [];

    public PaginaResultado<ProductoTarjeta> Resultado { get; private set; } = new();

    public bool HayFiltros => Categoria is not null || !string.IsNullOrWhiteSpace(Q);

    public async Task OnGetAsync()
    {
        Categorias = await _productoService.ObtenerCategoriasAsync();
        Resultado = await _productoService.ObtenerCatalogoAsync(Categoria, Q, Pagina);
    }
}
