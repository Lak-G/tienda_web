using Microsoft.EntityFrameworkCore;
using TiendaNube.Web.Models;

namespace TiendaNube.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Orden> Ordenes => Set<Orden>();
    public DbSet<DetalleOrden> DetallesOrden => Set<DetalleOrden>();
    public DbSet<Administrador> Administradores => Set<Administrador>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("Categorias");
            e.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
            e.HasIndex(c => c.Nombre).IsUnique();
        });

        modelBuilder.Entity<Producto>(e =>
        {
            e.ToTable("Productos", t =>
            {
                t.HasCheckConstraint("CK_Productos_Precio", "\"Precio\" > 0");
                t.HasCheckConstraint("CK_Productos_Stock", "\"Stock\" >= 0");
            });
            e.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
            e.Property(p => p.Descripcion).HasMaxLength(1000);
            e.Property(p => p.Precio).HasPrecision(10, 2);
            e.Property(p => p.ImagenUrl).HasMaxLength(500);
            // ValueGeneratedNever: EF siempre envía el valor; sin esto un Activo = false se insertaría como true
            e.Property(p => p.Activo).HasDefaultValue(true).ValueGeneratedNever();
            e.Property(p => p.FechaCreacion).HasDefaultValueSql("now()");
            e.Property(p => p.Version).IsRowVersion();

            e.HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("Clientes");
            e.Property(c => c.Nombre).HasMaxLength(150).IsRequired();
            e.Property(c => c.Correo).HasMaxLength(150).IsRequired();
            e.Property(c => c.Telefono).HasMaxLength(20);
            e.Property(c => c.Direccion).HasMaxLength(300);
        });

        modelBuilder.Entity<Orden>(e =>
        {
            e.ToTable("Ordenes", t => t.HasCheckConstraint("CK_Ordenes_Total", "\"Total\" >= 0"));
            e.Property(o => o.Fecha).HasDefaultValueSql("now()");
            e.Property(o => o.Total).HasPrecision(10, 2);
            e.Property(o => o.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

            e.HasOne(o => o.Cliente)
                .WithMany(c => c.Ordenes)
                .HasForeignKey(o => o.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetalleOrden>(e =>
        {
            e.ToTable("DetallesOrden", t =>
            {
                t.HasCheckConstraint("CK_DetallesOrden_Cantidad", "\"Cantidad\" > 0");
                t.HasCheckConstraint("CK_DetallesOrden_PrecioUnitario", "\"PrecioUnitario\" > 0");
                t.HasCheckConstraint("CK_DetallesOrden_Subtotal", "\"Subtotal\" >= 0");
            });
            e.Property(d => d.PrecioUnitario).HasPrecision(10, 2);
            e.Property(d => d.Subtotal).HasPrecision(10, 2);
            e.HasIndex(d => new { d.OrdenId, d.ProductoId }).IsUnique();

            e.HasOne(d => d.Orden)
                .WithMany(o => o.Detalles)
                .HasForeignKey(d => d.OrdenId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict: un producto con órdenes no se borra físicamente, se desactiva (Activo = false)
            e.HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Administrador>(e =>
        {
            e.ToTable("Administradores");
            e.Property(a => a.Usuario).HasMaxLength(50).IsRequired();
            e.Property(a => a.HashContrasena).IsRequired();
            e.HasIndex(a => a.Usuario).IsUnique();
        });
    }
}
