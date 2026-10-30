using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages.Admin.Productos;

public class EliminarModel(IProductoAdminService productoAdminService) : PageModel
{
    private readonly IProductoAdminService _productoAdminService = productoAdminService;

    public InfoEliminacionProducto Producto { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var producto = await _productoAdminService.ObtenerInfoEliminacionAsync(id);
        if (producto is null)
            return NotFound();

        Producto = producto;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resultado = await _productoAdminService.EliminarAsync(id);
        TempData[resultado == ResultadoEliminacion.NoEncontrado ? "Error" : "Mensaje"] = resultado switch
        {
            ResultadoEliminacion.Eliminado => "Producto eliminado.",
            ResultadoEliminacion.Desactivado => "El producto tiene órdenes registradas: se ocultó del catálogo en lugar de borrarse.",
            _ => "El producto ya no existe."
        };
        return RedirectToPage("Index");
    }
}
