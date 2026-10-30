using Microsoft.EntityFrameworkCore;
using TiendaNube.Web.Data;
using TiendaNube.Web.Models;
using TiendaNube.Web.Models.ViewModels;

namespace TiendaNube.Web.Services;

public class CategoriaService(AppDbContext db) : ICategoriaService
{
    private const int LargoMaximoNombre = 100;

    private readonly AppDbContext _db = db;

    public async Task<IReadOnlyList<CategoriaFila>> ListarAsync()
    {
        return await _db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaFila
            {
                Id = c.Id,
                Nombre = c.Nombre,
                CantidadProductos = c.Productos.Count
            })
            .ToListAsync();
    }

    public async Task<ResultadoOperacion> CrearAsync(string? nombre)
    {
        var (limpio, error) = await ValidarNombreAsync(nombre, idActual: null);
        if (error is not null)
            return ResultadoOperacion.Fallo(error);

        _db.Categorias.Add(new Categoria { Nombre = limpio! });
        return await GuardarAsync();
    }

    public async Task<ResultadoOperacion> RenombrarAsync(int id, string? nombre)
    {
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id);
        if (categoria is null)
            return ResultadoOperacion.Fallo("La categoría ya no existe.");

        var (limpio, error) = await ValidarNombreAsync(nombre, id);
        if (error is not null)
            return ResultadoOperacion.Fallo(error);

        categoria.Nombre = limpio!;
        return await GuardarAsync();
    }

    public async Task<ResultadoOperacion> EliminarAsync(int id)
    {
        var categoria = await _db.Categorias
            .Where(c => c.Id == id)
            .Select(c => new { Entidad = c, Productos = c.Productos.Count })
            .FirstOrDefaultAsync();

        if (categoria is null)
            return ResultadoOperacion.Fallo("La categoría ya no existe.");

        if (categoria.Productos > 0)
            return ResultadoOperacion.Fallo(
                $"«{categoria.Entidad.Nombre}» tiene {categoria.Productos} {(categoria.Productos == 1 ? "producto" : "productos")}. Muévelos a otra categoría antes de eliminarla.");

        _db.Categorias.Remove(categoria.Entidad);
        return await GuardarAsync();
    }

    private async Task<(string? Nombre, string? Error)> ValidarNombreAsync(string? nombre, int? idActual)
    {
        var limpio = nombre?.Trim();
        if (string.IsNullOrEmpty(limpio))
            return (null, "El nombre de la categoría es obligatorio.");

        if (limpio.Length > LargoMaximoNombre)
            return (null, $"El nombre no puede exceder {LargoMaximoNombre} caracteres.");

        var minusculas = limpio.ToLower();
        var duplicada = await _db.Categorias.AnyAsync(c => c.Nombre.ToLower() == minusculas && c.Id != idActual);
        if (duplicada)
            return (null, $"Ya existe una categoría llamada «{limpio}».");

        return (limpio, null);
    }

    private async Task<ResultadoOperacion> GuardarAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
            return ResultadoOperacion.Ok();
        }
        catch (DbUpdateException)
        {
            // Carrera con otra petición: el índice UNIQUE o la FK Restrict rechazaron el cambio
            return ResultadoOperacion.Fallo("No se pudo guardar: la categoría está duplicada o tiene productos asociados.");
        }
    }
}
