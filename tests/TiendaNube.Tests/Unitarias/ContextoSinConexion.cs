using Microsoft.EntityFrameworkCore;
using TiendaNube.Web.Data;

namespace TiendaNube.Tests.Unitarias;

internal static class ContextoSinConexion
{
    // Los servicios piden un AppDbContext en el constructor, pero los caminos que prueban
    // las pruebas unitarias terminan antes de consultar; EF no abre la conexión hasta entonces
    public static AppDbContext Crear() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=no-se-usa").Options);
}
