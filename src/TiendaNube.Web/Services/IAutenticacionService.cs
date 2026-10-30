using TiendaNube.Web.Models;

namespace TiendaNube.Web.Services;

public interface IAutenticacionService
{
    /// <summary>Devuelve el administrador si usuario y contraseña coinciden; si no, null.</summary>
    Task<Administrador?> ValidarCredencialesAsync(string usuario, string contrasena);
}
