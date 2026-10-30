using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages.Admin.Ordenes;

public class DetalleModel(IOrdenService ordenService) : PageModel
{
    private readonly IOrdenService _ordenService = ordenService;

    public OrdenResumen Orden { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var orden = await _ordenService.ObtenerResumenAsync(id);
        if (orden is null)
            return NotFound();

        Orden = orden;
        return Page();
    }
}
