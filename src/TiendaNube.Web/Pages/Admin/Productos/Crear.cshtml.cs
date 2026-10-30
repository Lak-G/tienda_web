using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages.Admin.Productos;

public class CrearModel(IProductoAdminService productoAdminService, IProductoService productoService) : PageModel
{
    private readonly IProductoAdminService _productoAdminService = productoAdminService;
    private readonly IProductoService _productoService = productoService;

    [BindProperty]
    public ProductoFormulario Producto { get; set; } = new();

    [BindProperty]
    public IFormFile? Imagen { get; set; }

    public async Task OnGetAsync()
    {
        await CargarCategoriasAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            var resultado = await _productoAdminService.CrearAsync(Producto, Imagen);
            if (resultado.Exito)
            {
                TempData["Mensaje"] = $"Producto «{Producto.Nombre.Trim()}» creado.";
                return RedirectToPage("Index");
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
