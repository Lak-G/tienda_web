using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages;

public class CarritoModel(ICarritoService carritoService, IProductoService productoService) : PageModel
{
    private const string ErrorCantidad = "La cantidad debe ser un número entero mayor a 0.";

    private readonly ICarritoService _carritoService = carritoService;
    private readonly IProductoService _productoService = productoService;

    public CarritoVista Carrito { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Carrito = await _carritoService.ObtenerVistaAsync();
    }

    public async Task<IActionResult> OnPostAgregarAsync(int productoId, int cantidad = 1)
    {
        // Si "cantidad" no es numérico el binding falla y el parámetro quedaría en su valor por defecto
        var resultado = ModelState.IsValid
            ? await _carritoService.AgregarAsync(productoId, cantidad)
            : ResultadoOperacion.Fallo(ErrorCantidad);

        if (!resultado.Exito)
        {
            TempData["Error"] = resultado.Error;
            return await _productoService.ObtenerDetalleAsync(productoId) is null
                ? RedirectToPage("/Index")
                : RedirectToPage("/Productos/Detalle", new { id = productoId });
        }

        TempData["Mensaje"] = "Producto agregado al carrito.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActualizarAsync(int productoId, int cantidad)
    {
        var resultado = ModelState.IsValid
            ? await _carritoService.ActualizarCantidadAsync(productoId, cantidad)
            : ResultadoOperacion.Fallo(ErrorCantidad);

        if (resultado.Exito)
            TempData["Mensaje"] = "Cantidad actualizada.";
        else
            TempData["Error"] = resultado.Error;

        return RedirectToPage();
    }

    public IActionResult OnPostQuitar(int productoId)
    {
        _carritoService.Quitar(productoId);
        TempData["Mensaje"] = "Producto quitado del carrito.";
        return RedirectToPage();
    }

    public IActionResult OnPostVaciar()
    {
        _carritoService.Vaciar();
        TempData["Mensaje"] = "Carrito vaciado.";
        return RedirectToPage();
    }
}
