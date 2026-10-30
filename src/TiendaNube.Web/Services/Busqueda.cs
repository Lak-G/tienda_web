namespace TiendaNube.Web.Services;

public static class Busqueda
{
    private const int LargoMaximo = 100;

    public static string? Normalizar(string? busqueda)
    {
        if (string.IsNullOrWhiteSpace(busqueda))
            return null;

        var termino = busqueda.Trim();
        return termino.Length > LargoMaximo ? termino[..LargoMaximo] : termino;
    }

    /// <summary>Patrón para ILIKE con '\' como escape; sin escapar, "%" o "_" coincidirían con todo.</summary>
    public static string PatronContiene(string termino) =>
        $"%{termino.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_")}%";
}
