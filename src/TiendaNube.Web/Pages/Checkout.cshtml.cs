using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages;

public class CheckoutModel(ICarritoService carritoService, IOrdenService ordenService) : PageModel
{
    public const string ClaveUltimaOrden = "UltimaOrden";

    private readonly ICarritoService _carritoService = carritoService;
    private readonly IOrdenService _ordenService = ordenService;

    [BindProperty]
    public DatosCliente Cliente { get; set; } = new();

    public CarritoVista Carrito { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Carrito = await _carritoService.ObtenerVistaAsync();
        return RedirigirSiNoSePuedePagar() ?? Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Carrito = await _carritoService.ObtenerVistaAsync();
        var redireccion = RedirigirSiNoSePuedePagar();
        if (redireccion is not null)
            return redireccion;

        if (!ModelState.IsValid)
            return Page();

        var resultado = await _ordenService.CrearOrdenAsync(Cliente, _carritoService.ObtenerItems());
        if (!resultado.Exito)
        {
            ModelState.AddModelError(string.Empty, resultado.Error!);
            Carrito = await _carritoService.ObtenerVistaAsync();
            return Page();
        }

        _carritoService.Vaciar();
        HttpContext.Session.SetInt32(ClaveUltimaOrden, resultado.OrdenId!.Value);
        return RedirectToPage("/Confirmacion", new { id = resultado.OrdenId });
    }

    private IActionResult? RedirigirSiNoSePuedePagar()
    {
        if (Carrito.EstaVacio)
            return RedirectToPage("/Carrito");

        if (!Carrito.PuedePagar)
        {
            TempData["Error"] = "Algunos productos ya no tienen stock suficiente. Ajusta las cantidades para continuar.";
            return RedirectToPage("/Carrito");
        }

        return null;
    }
}
