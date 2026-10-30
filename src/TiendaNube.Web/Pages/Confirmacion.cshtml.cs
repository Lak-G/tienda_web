using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages;

public class ConfirmacionModel(IOrdenService ordenService) : PageModel
{
    private readonly IOrdenService _ordenService = ordenService;

    public OrdenResumen Orden { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Solo quien hizo la compra (misma sesión) puede ver la confirmación; evita exponer datos de otros clientes por Id
        if (HttpContext.Session.GetInt32(CheckoutModel.ClaveUltimaOrden) != id)
            return NotFound();

        var orden = await _ordenService.ObtenerResumenAsync(id);
        if (orden is null)
            return NotFound();

        Orden = orden;
        return Page();
    }
}
