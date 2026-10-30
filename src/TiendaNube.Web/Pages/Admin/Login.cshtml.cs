using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TiendaNube.Web.Services;

namespace TiendaNube.Web.Pages.Admin;

public class LoginModel(IAutenticacionService autenticacionService) : PageModel
{
    private readonly IAutenticacionService _autenticacionService = autenticacionService;

    [BindProperty]
    [Required(ErrorMessage = "Escribe tu usuario.")]
    public string Usuario { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Admin/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var admin = await _autenticacionService.ValidarCredencialesAsync(Usuario, Contrasena);
        if (admin is null)
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return Page();
        }

        var identidad = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()), new Claim(ClaimTypes.Name, admin.Usuario)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));

        // Solo rutas locales: evita que un enlace ?ReturnUrl=https://otro-sitio redirija fuera tras el login
        return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl!) : RedirectToPage("/Admin/Index");
    }
}
