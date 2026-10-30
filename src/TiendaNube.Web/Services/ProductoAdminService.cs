using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TiendaNube.Web.Data;
using TiendaNube.Web.Models;
using TiendaNube.Web.Models.ViewModels;
using TiendaNube.Web.Services.Almacenamiento;

namespace TiendaNube.Web.Services;

public class ProductoAdminService(AppDbContext db, IAlmacenamientoImagenes almacenamiento, ILogger<ProductoAdminService> logger)
    : IProductoAdminService
{
    private readonly AppDbContext _db = db;
    private readonly IAlmacenamientoImagenes _almacenamiento = almacenamiento;
    private readonly ILogger<ProductoAdminService> _logger = logger;

    public async Task<PaginaResultado<ProductoAdminFila>> ListarAsync(string? busqueda, int pagina, bool ordenarPorStock = false, int tamanoPagina = 15)
    {
        var consulta = _db.Productos.AsNoTracking();

        var termino = Busqueda.Normalizar(busqueda);
        if (termino is not null)
        {
            var patron = Busqueda.PatronContiene(termino);
            consulta = consulta.Where(p => EF.Functions.ILike(p.Nombre, patron, @"\"));
        }

        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        var paginaActual = Math.Clamp(pagina, 1, totalPaginas);

        var ordenada = ordenarPorStock
            ? consulta.OrderByDescending(p => p.Activo).ThenBy(p => p.Stock).ThenBy(p => p.Nombre)
            : consulta.OrderBy(p => p.Nombre);

        var elementos = await ordenada
            .ThenBy(p => p.Id)
            .Skip((paginaActual - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(p => new ProductoAdminFila
            {
                Id = p.Id,
                Nombre = p.Nombre,
                CategoriaNombre = p.Categoria!.Nombre,
                Precio = p.Precio,
                Stock = p.Stock,
                Activo = p.Activo
            })
            .ToListAsync();

        return new PaginaResultado<ProductoAdminFila>
        {
            Elementos = elementos,
            PaginaActual = paginaActual,
            TotalPaginas = totalPaginas,
            TotalElementos = total
        };
    }

    public async Task<ProductoFormulario?> ObtenerParaEditarAsync(int id)
    {
        return await _db.Productos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductoFormulario
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Precio = p.Precio,
                Stock = p.Stock,
                CategoriaId = p.CategoriaId,
                ImagenUrl = p.ImagenUrl,
                Activo = p.Activo,
                Version = p.Version
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoOperacion> CrearAsync(ProductoFormulario datos, IFormFile? imagen)
    {
        var error = await ValidarAsync(datos);
        if (error is not null)
            return ResultadoOperacion.Fallo(error);

        var (imagenSubida, errorImagen) = await SubirImagenAsync(imagen);
        if (errorImagen is not null)
            return ResultadoOperacion.Fallo(errorImagen);

        var producto = new Producto();
        Aplicar(datos, producto, imagenSubida);
        _db.Productos.Add(producto);

        try
        {
            await _db.SaveChangesAsync();
            return ResultadoOperacion.Ok();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al crear el producto {Nombre}.", datos.Nombre);
            await EliminarImagenAsync(imagenSubida);
            return ResultadoOperacion.Fallo("No se pudo guardar el producto. Intenta de nuevo.");
        }
    }

    public async Task<ResultadoOperacion> ActualizarAsync(ProductoFormulario datos, IFormFile? imagen)
    {
        var error = await ValidarAsync(datos);
        if (error is not null)
            return ResultadoOperacion.Fallo(error);

        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == datos.Id);
        if (producto is null)
            return ResultadoOperacion.Fallo("El producto ya no existe.");

        var (imagenSubida, errorImagen) = await SubirImagenAsync(imagen);
        if (errorImagen is not null)
            return ResultadoOperacion.Fallo(errorImagen);

        // Se compara contra el xmin que vio el admin al abrir el formulario, no contra el recién leído:
        // así una compra que descontó stock mientras editaba no se pisa con el valor viejo
        _db.Entry(producto).Property(p => p.Version).OriginalValue = datos.Version;

        var imagenAnterior = producto.ImagenUrl;
        Aplicar(datos, producto, imagenSubida);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await EliminarImagenAsync(imagenSubida);
            return ResultadoOperacion.FalloPorConflicto(
                "El producto cambió mientras lo editabas (por ejemplo, una compra descontó stock). Se cargaron los datos actuales; revisa y guarda de nuevo.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al actualizar el producto {Id}.", datos.Id);
            await EliminarImagenAsync(imagenSubida);
            return ResultadoOperacion.Fallo("No se pudo guardar el producto. Intenta de nuevo.");
        }

        if (imagenAnterior is not null && imagenAnterior != producto.ImagenUrl)
            await EliminarImagenAsync(imagenAnterior);

        return ResultadoOperacion.Ok();
    }

    public async Task<InfoEliminacionProducto?> ObtenerInfoEliminacionAsync(int id)
    {
        return await _db.Productos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new InfoEliminacionProducto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Activo = p.Activo,
                OrdenesAsociadas = _db.DetallesOrden.Count(d => d.ProductoId == p.Id)
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoEliminacion> EliminarAsync(int id)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id);
        if (producto is null)
            return ResultadoEliminacion.NoEncontrado;

        if (await _db.DetallesOrden.AnyAsync(d => d.ProductoId == id))
            return await DesactivarAsync(id);

        var imagen = producto.ImagenUrl;
        _db.Productos.Remove(producto);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Una compra lo agregó a una orden justo después de revisar: la FK Restrict impide el borrado
            _db.ChangeTracker.Clear();
            return await DesactivarAsync(id);
        }

        await EliminarImagenAsync(imagen);
        return ResultadoEliminacion.Eliminado;
    }

    private async Task<ResultadoEliminacion> DesactivarAsync(int id)
    {
        await _db.Productos.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Activo, false));
        return ResultadoEliminacion.Desactivado;
    }

    private async Task<string?> ValidarAsync(ProductoFormulario datos)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(datos, new ValidationContext(datos), errores, validateAllProperties: true))
            return errores[0].ErrorMessage;

        if (!await _db.Categorias.AnyAsync(c => c.Id == datos.CategoriaId))
            return "La categoría seleccionada no existe.";

        return null;
    }

    private async Task<(string? Url, string? Error)> SubirImagenAsync(IFormFile? imagen)
    {
        if (imagen is null || imagen.Length == 0)
            return (null, null);

        var (formato, error) = await ValidadorImagen.ValidarAsync(imagen);
        if (formato is null)
            return (null, error);

        await using var flujo = imagen.OpenReadStream();
        return (await _almacenamiento.GuardarAsync(flujo, formato), null);
    }

    private async Task EliminarImagenAsync(string? url)
    {
        if (!string.IsNullOrEmpty(url))
            await _almacenamiento.EliminarAsync(url);
    }

    private static void Aplicar(ProductoFormulario datos, Producto producto, string? imagenSubida)
    {
        producto.Nombre = datos.Nombre.Trim();
        producto.Descripcion = string.IsNullOrWhiteSpace(datos.Descripcion) ? null : datos.Descripcion.Trim();
        producto.Precio = datos.Precio!.Value;
        producto.Stock = datos.Stock!.Value;
        producto.CategoriaId = datos.CategoriaId!.Value;
        producto.Activo = datos.Activo;
        // Un archivo subido tiene prioridad sobre la URL escrita a mano
        producto.ImagenUrl = imagenSubida ?? (string.IsNullOrWhiteSpace(datos.ImagenUrl) ? null : datos.ImagenUrl.Trim());
    }
}
