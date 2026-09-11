using Pulumi;
using Dotcom.Cloud.Infrastructure.Azure.Compute.ContainerApp;
using Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup;
using Dotcom.Cloud.Infrastructure.Azure.Core.Vnet;
using Dotcom.Cloud.Infrastructure.Common;

return await Deployment.RunAsync(() =>
{
    var config = new Config();
    string environment = config.Get("environment") ?? "dev";

    var extraTags = new CafTags
    {
        App = "example-container-app",
        Tier = "web",
        Criticality = "low",
        CostCenter = "poc",
        BusinessUnit = "shared"
    }.ToInputMap();

    var rg = new OrgResourceGroup("example-ca", new OrgResourceGroupArgs
    {
        Environment = environment,
        Location = "southafricanorth",
        ExtraTags = extraTags
    });

    var vnet = new OrgVnet("example-ca-network", new OrgVnetArgs
    {
        Environment = environment,
        ResourceGroup = rg,
        AddressSpace = "10.43.0.0/16",
        PublicSubnetCidr = "10.43.1.0/24",
        PrivateSubnetCidr = "10.43.2.0/24",
        ExtraTags = extraTags
    });

    // SubnetId must be the ACA-delegated private subnet.
    var containerEnv = new OrgContainerEnvironment("example-env", new OrgContainerEnvironmentArgs
    {
        Environment = environment,
        ResourceGroup = rg,
        SubnetId = vnet.PrivateSubnetId,
        ExtraTags = extraTags
    });

    // Org default: Internal environment, no internet FQDN.
    // Set Internal = false and ExternalIngress = true only for an explicit public PoC.
    // MCR image — do not set Registry. For a private ACR image see azure-container-registry
    // and pass Registry = registry plus Image = Output.Format($"{registry.LoginServer}/app:1").
    var app = new OrgContainerApp("example-app", new OrgContainerAppArgs
    {
        Environment = environment,
        ResourceGroup = rg,
        EnvironmentId = containerEnv.EnvironmentId,
        Image = "mcr.microsoft.com/k8s/core/hello-world:latest",
        TargetPort = 80,
        ExtraTags = extraTags
    });

    return new Dictionary<string, object?>
    {
        ["ResourceGroupName"] = rg.Name,
        ["EnvironmentId"] = containerEnv.EnvironmentId,
        ["ContainerAppFqdn"] = app.Fqdn.Apply(fqdn => string.IsNullOrEmpty(fqdn) ? fqdn : $"https://{fqdn}")
    };
});
