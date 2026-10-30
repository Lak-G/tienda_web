using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages.Admin.Categorias;

public class IndexModel(ICategoriaService categoriaService) : PageModel
{
    private readonly ICategoriaService _categoriaService = categoriaService;

    public IReadOnlyList<CategoriaFila> Categorias { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Categorias = await _categoriaService.ListarAsync();
    }

    public async Task<IActionResult> OnPostCrearAsync(string? nombre) =>
        Responder(await _categoriaService.CrearAsync(nombre), "Categoría creada.");

    public async Task<IActionResult> OnPostRenombrarAsync(int id, string? nombre) =>
        Responder(await _categoriaService.RenombrarAsync(id, nombre), "Categoría renombrada.");

    public async Task<IActionResult> OnPostEliminarAsync(int id) =>
        Responder(await _categoriaService.EliminarAsync(id), "Categoría eliminada.");

    private IActionResult Responder(ResultadoOperacion resultado, string mensajeExito)
    {
        if (resultado.Exito)
            TempData["Mensaje"] = mensajeExito;
        else
            TempData["Error"] = resultado.Error;

        return RedirectToPage();
    }
}
