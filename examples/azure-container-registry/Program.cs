using Pulumi;
using Dotcom.Cloud.Infrastructure.Azure.Containers.Registry;
using Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup;
using Dotcom.Cloud.Infrastructure.Azure.Core.Vnet;
using Dotcom.Cloud.Infrastructure.Common;

return await Deployment.RunAsync(() =>
{
    var config = new Config();
    string environment = config.Get("environment") ?? "dev";

    var extraTags = new CafTags
    {
        App = "example-acr",
        Tier = "platform",
        Criticality = "low",
        CostCenter = "poc",
        BusinessUnit = "shared"
    }.ToInputMap();

    var rg = new OrgResourceGroup("example-acr", new OrgResourceGroupArgs
    {
        Environment = environment,
        Location = "southafricanorth",
        ExtraTags = extraTags
    });

    var vnet = new OrgVnet("example-acr-network", new OrgVnetArgs
    {
        Environment = environment,
        ResourceGroup = rg,
        AddressSpace = "10.42.0.0/16",
        PublicSubnetCidr = "10.42.1.0/24",
        PrivateSubnetCidr = "10.42.2.0/24",
        PrivateEndpointSubnetCidr = "10.42.3.0/24",
        ExtraTags = extraTags
    });

    var registry = new OrgContainerRegistry("example", new OrgContainerRegistryArgs
    {
        Environment = environment,
        ResourceGroup = rg,
        PrivateEndpointSubnetId = vnet.PrivateEndpointSubnetId,
        VirtualNetworkId = vnet.VnetId,
        ExtraTags = extraTags
    });

    return new Dictionary<string, object?>
    {
        ["ResourceGroupName"] = rg.Name,
        ["LoginServer"] = registry.LoginServer,
        ["RegistryName"] = registry.Name
    };
});
