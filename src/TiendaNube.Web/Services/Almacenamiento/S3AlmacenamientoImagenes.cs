using Amazon.S3;
using Amazon.S3.Model;

namespace TiendaNube.Web.Services.Almacenamiento;

/// <summary>
/// Producción: sube a s3://{Bucket}/productos/{guid}.{ext}. Las credenciales vienen de la cadena
/// por defecto del SDK (en EC2, el rol de la instancia TiendaNube-EC2-Role); nunca hay access keys en el código.
/// </summary>
public class S3AlmacenamientoImagenes(IAmazonS3 s3, IConfiguration configuracion, ILogger<S3AlmacenamientoImagenes> logger)
    : IAlmacenamientoImagenes
{
    private const string Prefijo = "productos/";

    private readonly IAmazonS3 _s3 = s3;
    private readonly ILogger<S3AlmacenamientoImagenes> _logger = logger;
    private readonly string _bucket = configuracion["S3:Bucket"]
        ?? throw new InvalidOperationException("Falta la configuración S3:Bucket.");
    private readonly string _region = configuracion["S3:Region"]
        ?? throw new InvalidOperationException("Falta la configuración S3:Region.");

    private string UrlBase => $"https://{_bucket}.s3.{_region}.amazonaws.com/";

    public async Task<string> GuardarAsync(Stream contenido, FormatoImagen formato)
    {
        var clave = $"{Prefijo}{Guid.NewGuid():N}.{formato.Extension}";

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = clave,
            InputStream = contenido,
            ContentType = formato.ContentType
        });

        return UrlBase + clave;
    }

    public async Task EliminarAsync(string url)
    {
        if (!url.StartsWith(UrlBase + Prefijo, StringComparison.Ordinal))
            return;

        try
        {
            await _s3.DeleteObjectAsync(_bucket, url[UrlBase.Length..]);
        }
        catch (AmazonS3Exception ex)
        {
            // Una imagen huérfana no debe impedir editar o borrar el producto
            _logger.LogWarning(ex, "No se pudo borrar la imagen {Url} de S3.", url);
        }
    }
}
