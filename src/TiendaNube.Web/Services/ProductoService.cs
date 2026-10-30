using Microsoft.EntityFrameworkCore;
using TiendaNube.Web.Data;
using TiendaNube.Web.Models;
using TiendaNube.Web.Models.ViewModels;

namespace TiendaNube.Web.Services;

public class ProductoService(AppDbContext db) : IProductoService
{
    private readonly AppDbContext _db = db;

    public async Task<IReadOnlyList<Categoria>> ObtenerCategoriasAsync()
    {
        return await _db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<PaginaResultado<ProductoTarjeta>> ObtenerCatalogoAsync(int? categoriaId, string? busqueda, int pagina, int tamanoPagina = 9)
    {
        var consulta = _db.Productos.AsNoTracking().Where(p => p.Activo);

        if (categoriaId is not null)
            consulta = consulta.Where(p => p.CategoriaId == categoriaId);

        var termino = Busqueda.Normalizar(busqueda);
        if (termino is not null)
        {
            var patron = Busqueda.PatronContiene(termino);
            consulta = consulta.Where(p =>
                EF.Functions.ILike(p.Nombre, patron, @"\") ||
                (p.Descripcion != null && EF.Functions.ILike(p.Descripcion, patron, @"\")));
        }

        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        var paginaActual = Math.Clamp(pagina, 1, totalPaginas);

        var elementos = await consulta
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Id)
            .Skip((paginaActual - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(p => new ProductoTarjeta
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Precio = p.Precio,
                Stock = p.Stock,
                ImagenUrl = p.ImagenUrl,
                CategoriaNombre = p.Categoria!.Nombre
            })
            .ToListAsync();

        return new PaginaResultado<ProductoTarjeta>
        {
            Elementos = elementos,
            PaginaActual = paginaActual,
            TotalPaginas = totalPaginas,
            TotalElementos = total
        };
    }

    public async Task<ProductoDetalle?> ObtenerDetalleAsync(int id)
    {
        return await _db.Productos
            .AsNoTracking()
            .Where(p => p.Id == id && p.Activo)
            .Select(p => new ProductoDetalle
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Precio = p.Precio,
                Stock = p.Stock,
                ImagenUrl = p.ImagenUrl,
                CategoriaId = p.CategoriaId,
                CategoriaNombre = p.Categoria!.Nombre
            })
            .FirstOrDefaultAsync();
    }
}
