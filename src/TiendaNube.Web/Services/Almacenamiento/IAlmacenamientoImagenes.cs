namespace TiendaNube.Web.Services.Almacenamiento;

public interface IAlmacenamientoImagenes
{
    /// <summary>Guarda la imagen y devuelve la URL pública con la que se mostrará.</summary>
    Task<string> GuardarAsync(Stream contenido, FormatoImagen formato);

    /// <summary>Borra una imagen guardada por este almacenamiento; ignora URLs externas.</summary>
    Task EliminarAsync(string url);
}
