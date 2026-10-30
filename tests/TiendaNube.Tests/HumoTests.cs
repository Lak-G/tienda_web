using TiendaNube.Web.Models.ViewModels;

namespace TiendaNube.Tests;

public class HumoTests
{
    [Fact]
    public void PaginaResultado_EnPaginaIntermedia_TieneAnteriorYSiguiente()
    {
        var resultado = new PaginaResultado<int> { PaginaActual = 2, TotalPaginas = 3 };

        Assert.True(resultado.TieneAnterior);
        Assert.True(resultado.TieneSiguiente);
    }
}
