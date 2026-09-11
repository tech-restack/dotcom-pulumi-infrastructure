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
    public void GetWindowsComputerName_is_alphanumeric_max_15()
    {
        Assert.Equal("vmdevwebfronten", NamingConventions.GetWindowsComputerName("web-frontend", "dev"));
    }

    [Fact]
    public void GetResourceName_vm_nic_disk_prefixes()
    {
        Assert.Equal("vm-dev-api", NamingConventions.GetResourceName("api", "dev", "vm"));
        Assert.Equal("nic-dev-api", NamingConventions.GetResourceName("api", "dev", "nic"));
        Assert.Equal("disk-dev-api", NamingConventions.GetResourceName("api", "dev", "disk"));
    }
}
