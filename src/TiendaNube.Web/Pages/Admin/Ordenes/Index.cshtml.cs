using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages.Admin.Ordenes;

public class IndexModel(IOrdenService ordenService) : PageModel
{
    private readonly IOrdenService _ordenService = ordenService;

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    public PaginaResultado<OrdenFila> Resultado { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Resultado = await _ordenService.ListarAsync(Pagina);
    }
}
