namespace TiendaNube.Web.Services.Almacenamiento;

public record FormatoImagen(string Extension, string ContentType);

public static class ValidadorImagen
{
    public const long TamanoMaximo = 2 * 1024 * 1024;

    private static readonly FormatoImagen Jpeg = new("jpg", "image/jpeg");
    private static readonly FormatoImagen Png = new("png", "image/png");
    private static readonly FormatoImagen Webp = new("webp", "image/webp");
    private static readonly FormatoImagen Gif = new("gif", "image/gif");

    /// <summary>
    /// Valida tamaño y tipo. El tipo se decide por la firma del archivo, no por el Content-Type
    /// que manda el navegador: así nadie sube HTML o SVG renombrado al bucket público.
    /// </summary>
    public static async Task<(FormatoImagen? Formato, string? Error)> ValidarAsync(IFormFile archivo)
    {
        if (archivo.Length == 0)
            return (null, "El archivo de imagen está vacío.");

        if (archivo.Length > TamanoMaximo)
            return (null, "La imagen no puede pesar más de 2 MB.");

        if (!archivo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return (null, "El archivo debe ser una imagen.");

        var cabecera = new byte[12];
        await using var flujo = archivo.OpenReadStream();
        var leidos = await flujo.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false);

        var formato = DetectarFormato(cabecera.AsSpan(0, leidos));
        return formato is null
            ? (null, "Formato no admitido. Usa JPG, PNG, WebP o GIF.")
            : (formato, null);
    }

    public static FormatoImagen? DetectarFormato(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return Jpeg;
        if (b.Length >= 8 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return Png;
        if (b.Length >= 12 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8))
            return Webp;
        if (b.Length >= 6 && (b[..6].SequenceEqual("GIF87a"u8) || b[..6].SequenceEqual("GIF89a"u8)))
            return Gif;
        return null;
    }
}
