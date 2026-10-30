namespace TiendaNube.Web.Services;

/// <summary>Las fechas se guardan en UTC; se muestran en la zona configurada (ZonaHoraria, IANA).</summary>
public class HoraLocal(IConfiguration configuracion)
{
    private readonly TimeZoneInfo _zona = TimeZoneInfo.FindSystemTimeZoneById(configuracion["ZonaHoraria"] ?? "America/Tijuana");

    public DateTime Convertir(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _zona);

    public string Formatear(DateTime utc) => Convertir(utc).ToString("dd/MM/yyyy HH:mm");
}
