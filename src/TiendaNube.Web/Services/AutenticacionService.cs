using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaNube.Web.Data;
using TiendaNube.Web.Models;

namespace TiendaNube.Web.Services;

public class AutenticacionService(AppDbContext db) : IAutenticacionService
{
    private readonly AppDbContext _db = db;
    private readonly PasswordHasher<Administrador> _hasher = new();

    public async Task<Administrador?> ValidarCredencialesAsync(string usuario, string contrasena)
    {
        var admin = await _db.Administradores.FirstOrDefaultAsync(a => a.Usuario == usuario.Trim());
        if (admin is null)
        {
            // Mismo costo de hash que un usuario existente, para no revelar qué usuarios existen por el tiempo de respuesta
            _hasher.HashPassword(new Administrador(), contrasena);
            return null;
        }

        var resultado = _hasher.VerifyHashedPassword(admin, admin.HashContrasena, contrasena);
        if (resultado == PasswordVerificationResult.Failed)
            return null;

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            admin.HashContrasena = _hasher.HashPassword(admin, contrasena);
            await _db.SaveChangesAsync();
        }

        return admin;
    }
}
