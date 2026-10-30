using System.ComponentModel.DataAnnotations;

namespace TiendaNube.Web.Models.ViewModels;

public class ProductoFormulario : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede exceder {1} caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción no puede exceder {1} caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true,
        ErrorMessage = "El precio debe estar entre $0.01 y $99,999,999.99.")]
    public decimal? Precio { get; set; }

    [Required(ErrorMessage = "El stock es obligatorio.")]
    [Range(0, 1_000_000, ErrorMessage = "El stock debe estar entre 0 y 1,000,000.")]
    public int? Stock { get; set; }

    [Required(ErrorMessage = "Selecciona una categoría.")]
    [Display(Name = "Categoría")]
    public int? CategoriaId { get; set; }

    [StringLength(500, ErrorMessage = "La URL no puede exceder {1} caracteres.")]
    [Display(Name = "URL de imagen")]
    public string? ImagenUrl { get; set; }

    [Display(Name = "Visible en el catálogo")]
    public bool Activo { get; set; } = true;

    // xmin leído al abrir el formulario; si cambió al guardar, alguien más modificó el producto
    public uint Version { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Las imágenes subidas en desarrollo se guardan con ruta relativa del sitio
        if (string.IsNullOrWhiteSpace(ImagenUrl) || ImagenUrl.StartsWith("/uploads/productos/", StringComparison.Ordinal))
            yield break;

        if (!Uri.TryCreate(ImagenUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            yield return new ValidationResult("La URL de imagen debe empezar con http:// o https://.", [nameof(ImagenUrl)]);
        }
    }
}
