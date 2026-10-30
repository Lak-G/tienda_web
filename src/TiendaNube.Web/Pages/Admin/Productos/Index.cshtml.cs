using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages.Admin.Productos;

public class IndexModel(IProductoAdminService productoAdminService) : PageModel
{
    private readonly IProductoAdminService _productoAdminService = productoAdminService;

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    public PaginaResultado<ProductoAdminFila> Resultado { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Resultado = await _productoAdminService.ListarAsync(Q, Pagina);
    }
}
