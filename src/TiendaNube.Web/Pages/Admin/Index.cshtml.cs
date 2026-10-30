using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages.Admin;

/// <summary>Vista de stock actual: productos activos primero, de menor a mayor existencia.</summary>
public class IndexModel(IProductoAdminService productoAdminService) : PageModel
{
    public const int StockBajo = 5;

    private readonly IProductoAdminService _productoAdminService = productoAdminService;

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    public PaginaResultado<ProductoAdminFila> Resultado { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Resultado = await _productoAdminService.ListarAsync(null, Pagina, ordenarPorStock: true, tamanoPagina: 20);
    }
}
