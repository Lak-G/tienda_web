using TiendaNube.Web.Pages;

namespace TiendaNube.Tests;

public class HumoTests
{
    [Fact]
    public void ProyectoWeb_SeReferenciaDesdeLasPruebas()
    {
        Assert.NotNull(new IndexModel());
    }
}
