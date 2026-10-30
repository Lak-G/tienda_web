namespace TiendaNube.Web.Models;

public class Administrador
{
    public int Id { get; set; }

    public string Usuario { get; set; } = string.Empty;

    public string HashContrasena { get; set; } = string.Empty;
}
