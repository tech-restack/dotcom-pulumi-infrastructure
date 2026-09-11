using Dotcom.Cloud.Infrastructure.Common;

namespace Dotcom.Cloud.Tests;

public class NamingConventionsTests
{
    [Fact]
    public void GetResourceName_is_lowercase_type_env_component()
    {
        Assert.Equal("rg-dev-web-frontend", NamingConventions.GetResourceName("web-frontend", "dev", "rg"));
    }

    [Fact]
    public void GetAcrName_strips_hyphens_and_stays_alphanumeric()
    {
        Assert.Equal("acrdevapppocdev", NamingConventions.GetAcrName("app-poc-dev", "dev"));
    }
}
