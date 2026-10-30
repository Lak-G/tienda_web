using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TiendaNube.Web.Data;
using TiendaNube.Web.Models.ViewModels;

namespace TiendaNube.Web.Services;

public class CarritoService(AppDbContext db, IHttpContextAccessor httpContextAccessor) : ICarritoService
{
    private const string ClaveSesion = "Carrito";

    private readonly AppDbContext _db = db;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private ISession Sesion => _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("El carrito requiere una sesión HTTP activa.");

    public IReadOnlyList<ItemCarrito> ObtenerItems() => LeerItems();

    public int ContarArticulos() => LeerItems().Sum(i => i.Cantidad);

    public async Task<ResultadoOperacion> AgregarAsync(int productoId, int cantidad)
    {
        if (cantidad <= 0)
            return ResultadoOperacion.Fallo("La cantidad debe ser mayor a 0.");

        var producto = await BuscarProductoActivoAsync(productoId);
        if (producto is null)
            return ResultadoOperacion.Fallo("El producto no existe o ya no está disponible.");

        var items = LeerItems();
        var item = items.Find(i => i.ProductoId == productoId);
        var cantidadEnCarrito = item?.Cantidad ?? 0;

        if (producto.Stock == 0)
            return ResultadoOperacion.Fallo($"«{producto.Nombre}» está agotado.");

        if ((long)cantidadEnCarrito + cantidad > producto.Stock)
        {
            var disponibles = Math.Max(0, producto.Stock - cantidadEnCarrito);
            return ResultadoOperacion.Fallo(cantidadEnCarrito > 0
                ? $"Solo hay {producto.Stock} unidades de «{producto.Nombre}» y ya tienes {cantidadEnCarrito} en el carrito; puedes agregar {disponibles} más."
                : $"Solo hay {producto.Stock} unidades disponibles de «{producto.Nombre}».");
        }

        if (item is null)
            items.Add(new ItemCarrito { ProductoId = productoId, Cantidad = cantidad });
        else
            item.Cantidad += cantidad;

        GuardarItems(items);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> ActualizarCantidadAsync(int productoId, int cantidad)
    {
        if (cantidad <= 0)
            return ResultadoOperacion.Fallo("La cantidad debe ser mayor a 0. Para quitar el producto usa «Quitar».");

        var items = LeerItems();
        var item = items.Find(i => i.ProductoId == productoId);
        if (item is null)
            return ResultadoOperacion.Fallo("El producto no está en el carrito.");

        var producto = await BuscarProductoActivoAsync(productoId);
        if (producto is null)
            return ResultadoOperacion.Fallo("El producto ya no está disponible.");

        if (cantidad > producto.Stock)
            return ResultadoOperacion.Fallo(producto.Stock == 0
                ? $"«{producto.Nombre}» está agotado. Quítalo del carrito para continuar."
                : $"Solo hay {producto.Stock} unidades disponibles de «{producto.Nombre}».");

        item.Cantidad = cantidad;
        GuardarItems(items);
        return ResultadoOperacion.Ok();
    }

    public void Quitar(int productoId)
    {
        var items = LeerItems();
        if (items.RemoveAll(i => i.ProductoId == productoId) > 0)
            GuardarItems(items);
    }

    public void Vaciar() => Sesion.Remove(ClaveSesion);

    public async Task<CarritoVista> ObtenerVistaAsync()
    {
        var items = LeerItems();
        if (items.Count == 0)
            return new CarritoVista();

        var ids = items.Select(i => i.ProductoId).ToList();
        var productos = await _db.Productos
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.Activo)
            .Select(p => new { p.Id, p.Nombre, p.ImagenUrl, p.Precio, p.Stock })
            .ToDictionaryAsync(p => p.Id);

        var lineas = new List<LineaCarrito>();
        var avisos = new List<string>();
        foreach (var item in items)
        {
            if (!productos.TryGetValue(item.ProductoId, out var producto))
            {
                avisos.Add("Se quitó del carrito un producto que ya no está disponible.");
                continue;
            }

            lineas.Add(new LineaCarrito
            {
                ProductoId = producto.Id,
                Nombre = producto.Nombre,
                ImagenUrl = producto.ImagenUrl,
                PrecioUnitario = producto.Precio,
                Cantidad = item.Cantidad,
                StockDisponible = producto.Stock
            });
        }

        if (avisos.Count > 0)
            GuardarItems(items.Where(i => productos.ContainsKey(i.ProductoId)).ToList());

        return new CarritoVista { Lineas = lineas, Avisos = avisos };
    }

    private Task<ProductoStock?> BuscarProductoActivoAsync(int productoId) =>
        _db.Productos
            .AsNoTracking()
            .Where(p => p.Id == productoId && p.Activo)
            .Select(p => new ProductoStock(p.Nombre, p.Stock))
            .FirstOrDefaultAsync();

    private List<ItemCarrito> LeerItems()
    {
        var json = Sesion.GetString(ClaveSesion);
        if (string.IsNullOrEmpty(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<ItemCarrito>>(json) ?? [];
        }
        catch (JsonException)
        {
            Sesion.Remove(ClaveSesion);
            return [];
        }
    }

    private void GuardarItems(List<ItemCarrito> items)
    {
        if (items.Count == 0)
            Sesion.Remove(ClaveSesion);
        else
            Sesion.SetString(ClaveSesion, JsonSerializer.Serialize(items));
    }

    private sealed record ProductoStock(string Nombre, int Stock);
}
