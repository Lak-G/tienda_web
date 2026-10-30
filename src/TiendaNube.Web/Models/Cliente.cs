using System.ComponentModel.DataAnnotations;

namespace TiendaNube.Web.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede exceder {1} caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(150, ErrorMessage = "El correo no puede exceder {1} caracteres.")]
    public string Correo { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El teléfono no es válido.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede exceder {1} caracteres.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(300, ErrorMessage = "La dirección no puede exceder {1} caracteres.")]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    public ICollection<Orden> Ordenes { get; set; } = new List<Orden>();
}
