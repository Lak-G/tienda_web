namespace TiendaNube.Web.Services.Almacenamiento;

/// <summary>Desarrollo: guarda en wwwroot/uploads/productos.</summary>
public class LocalAlmacenamientoImagenes(IWebHostEnvironment entorno) : IAlmacenamientoImagenes
{
    private const string PrefijoUrl = "/uploads/productos/";

    private readonly string _carpeta = Path.Combine(entorno.WebRootPath, "uploads", "productos");

    public async Task<string> GuardarAsync(Stream contenido, FormatoImagen formato)
    {
        Directory.CreateDirectory(_carpeta);
        var nombre = $"{Guid.NewGuid():N}.{formato.Extension}";

        await using var destino = File.Create(Path.Combine(_carpeta, nombre));
        await contenido.CopyToAsync(destino);

        return PrefijoUrl + nombre;
    }

    public Task EliminarAsync(string url)
    {
        if (!url.StartsWith(PrefijoUrl, StringComparison.Ordinal))
            return Task.CompletedTask;

        // GetFileName descarta cualquier segmento de ruta ("../") que viniera en la URL
        var ruta = Path.Combine(_carpeta, Path.GetFileName(url));
        if (File.Exists(ruta))
            File.Delete(ruta);

        return Task.CompletedTask;
    }
}
