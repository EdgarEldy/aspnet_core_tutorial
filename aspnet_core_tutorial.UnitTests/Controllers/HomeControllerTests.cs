using aspnet_core_tutorial.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace aspnet_core_tutorial.UnitTests.Controllers;

public class HomeControllerTests
{
    private static HomeController CreateController() => new(NullLogger<HomeController>.Instance);

    [Fact]
    public void Index_Returns_View()
    {
        var controller = CreateController();

        var result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Privacy_Returns_View()
    {
        var controller = CreateController();

        var result = controller.Privacy();

        Assert.IsType<ViewResult>(result);
    }
}
