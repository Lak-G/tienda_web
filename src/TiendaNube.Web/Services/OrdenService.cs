using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TiendaNube.Web.Data;
using TiendaNube.Web.Models;
using TiendaNube.Web.Models.ViewModels;

namespace TiendaNube.Web.Services;

public class OrdenService(AppDbContext db, ILogger<OrdenService> logger) : IOrdenService
{
    private const int MaxReintentos = 3;

    private readonly AppDbContext _db = db;
    private readonly ILogger<OrdenService> _logger = logger;

    public async Task<ResultadoOrden> CrearOrdenAsync(DatosCliente datos, IReadOnlyList<ItemCarrito> items)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(datos, new ValidationContext(datos), errores, validateAllProperties: true))
            return ResultadoOrden.Fallo(errores[0].ErrorMessage ?? "Los datos del cliente no son válidos.");

        if (items.Count == 0)
            return ResultadoOrden.Fallo("El carrito está vacío.");

        if (items.Any(i => i.Cantidad <= 0))
            return ResultadoOrden.Fallo("Todas las cantidades deben ser mayores a 0.");

        var cantidades = items
            .GroupBy(i => i.ProductoId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Cantidad));

        for (var intento = 0; ; intento++)
        {
            // Cada intento relee el stock desde cero; sin esto se reusarían entidades con el xmin viejo
            _db.ChangeTracker.Clear();
            await using var transaccion = await _db.Database.BeginTransactionAsync();

            try
            {
                var ids = cantidades.Keys.ToList();
                var productos = await _db.Productos
                    .Where(p => ids.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                var orden = new Orden
                {
                    Cliente = new Cliente
                    {
                        Nombre = datos.Nombre.Trim(),
                        Correo = datos.Correo.Trim(),
                        Telefono = string.IsNullOrWhiteSpace(datos.Telefono) ? null : datos.Telefono.Trim(),
                        Direccion = string.IsNullOrWhiteSpace(datos.Direccion) ? null : datos.Direccion.Trim()
                    },
                    Fecha = DateTime.UtcNow,
                    Estado = EstadoOrden.Pendiente
                };

                foreach (var (productoId, cantidad) in cantidades)
                {
                    if (!productos.TryGetValue(productoId, out var producto) || !producto.Activo)
                    {
                        var nombre = producto?.Nombre;
                        return ResultadoOrden.Fallo(nombre is null
                            ? "Un producto de tu carrito ya no existe. Revisa el carrito."
                            : $"«{nombre}» ya no está disponible. Quítalo del carrito para continuar.");
                    }

                    if (producto.Stock < cantidad)
                    {
                        return ResultadoOrden.Fallo(producto.Stock == 0
                            ? $"«{producto.Nombre}» se agotó. Quítalo del carrito para continuar."
                            : $"No hay stock suficiente de «{producto.Nombre}»: pediste {cantidad} y quedan {producto.Stock}.");
                    }

                    producto.Stock -= cantidad;
                    orden.Detalles.Add(new DetalleOrden
                    {
                        ProductoId = producto.Id,
                        Cantidad = cantidad,
                        PrecioUnitario = producto.Precio,
                        Subtotal = producto.Precio * cantidad
                    });
                }

                orden.Total = orden.Detalles.Sum(d => d.Subtotal);
                _db.Ordenes.Add(orden);

                // El UPDATE de cada producto incluye WHERE xmin = <leído>; si otra compra lo modificó
                // entre la lectura y este punto, afecta 0 filas y EF lanza DbUpdateConcurrencyException
                await _db.SaveChangesAsync();
                await transaccion.CommitAsync();

                return ResultadoOrden.Ok(orden.Id);
            }
            catch (DbUpdateConcurrencyException) when (intento < MaxReintentos)
            {
                await transaccion.RollbackAsync();
                _logger.LogInformation("Conflicto de concurrencia en checkout; reintento {Intento} de {Max}.", intento + 1, MaxReintentos);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Checkout abortado tras {Max} reintentos por conflictos de concurrencia.", MaxReintentos);
                return ResultadoOrden.Fallo("Otros clientes están comprando los mismos productos en este momento. Revisa tu carrito e intenta de nuevo.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al crear la orden.");
                return ResultadoOrden.Fallo("No se pudo registrar la orden. Intenta de nuevo.");
            }
        }
    }

    public async Task<OrdenResumen?> ObtenerResumenAsync(int ordenId)
    {
        return await _db.Ordenes
            .AsNoTracking()
            .Where(o => o.Id == ordenId)
            .Select(o => new OrdenResumen
            {
                Id = o.Id,
                Fecha = o.Fecha,
                Total = o.Total,
                Estado = o.Estado,
                ClienteNombre = o.Cliente!.Nombre,
                ClienteCorreo = o.Cliente.Correo,
                Lineas = o.Detalles
                    .OrderBy(d => d.Id)
                    .Select(d => new LineaOrden
                    {
                        Producto = d.Producto!.Nombre,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Subtotal = d.Subtotal
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }
}
