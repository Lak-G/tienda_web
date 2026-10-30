using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages.Admin.Productos;

public class EditarModel(IProductoAdminService productoAdminService, IProductoService productoService) : PageModel
{
    private readonly IProductoAdminService _productoAdminService = productoAdminService;
    private readonly IProductoService _productoService = productoService;

    [BindProperty]
    public ProductoFormulario Producto { get; set; } = new();

    [BindProperty]
    public IFormFile? Imagen { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var producto = await _productoAdminService.ObtenerParaEditarAsync(id);
        if (producto is null)
            return NotFound();

        Producto = producto;
        await CargarCategoriasAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Producto.Id = id;

        if (ModelState.IsValid)
        {
            var resultado = await _productoAdminService.ActualizarAsync(Producto, Imagen);
            if (resultado.Exito)
            {
                TempData["Mensaje"] = $"Producto «{Producto.Nombre.Trim()}» actualizado.";
                return RedirectToPage("Index");
            }

            if (resultado.Conflicto)
            {
                // Se muestran los valores actuales de la BD (con su nuevo xmin) en lugar de lo que envió el formulario
                var actual = await _productoAdminService.ObtenerParaEditarAsync(id);
                if (actual is null)
                    return NotFound();

                ModelState.Clear();
                Producto = actual;
            }

            ModelState.AddModelError(string.Empty, resultado.Error!);
        }

        await CargarCategoriasAsync();
        return Page();
    }

    private async Task CargarCategoriasAsync()
    {
        var categorias = await _productoService.ObtenerCategoriasAsync();
        ViewData["Categorias"] = categorias.Select(c => new SelectListItem(c.Nombre, c.Id.ToString())).ToList();
    }
}
