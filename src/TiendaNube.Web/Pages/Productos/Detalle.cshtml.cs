using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages.Productos;

public class DetalleModel(IProductoService productoService) : PageModel
{
    private readonly IProductoService _productoService = productoService;

    public ProductoDetalle Producto { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var producto = await _productoService.ObtenerDetalleAsync(id);
        if (producto is null)
            return NotFound();

        Producto = producto;
        return Page();
    }
}
