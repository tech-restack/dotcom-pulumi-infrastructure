using Pulumi;
using Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup;
using Dotcom.Cloud.Infrastructure.Azure.Core.Vnet;
using Dotcom.Cloud.Infrastructure.Common;

return await Deployment.RunAsync(() =>
{
    var config = new Config();
    string environment = config.Get("environment") ?? "dev";

    var extraTags = new CafTags
    {
        App = "example-vnet",
        Tier = "network",
        Criticality = "low",
        CostCenter = "poc",
        BusinessUnit = "shared"
    }.ToInputMap();

    var rg = new OrgResourceGroup("example-vnet", new OrgResourceGroupArgs
    {
        Environment = environment,
        Location = "southafricanorth",
        ExtraTags = extraTags
    });

    var vnet = new OrgVnet("example-network", new OrgVnetArgs
    {
        Environment = environment,
        ResourceGroup = rg,
        AddressSpace = "10.41.0.0/16",
        PublicSubnetCidr = "10.41.1.0/24",
        PrivateSubnetCidr = "10.41.2.0/24",
        PrivateEndpointSubnetCidr = "10.41.3.0/24",
        ExtraTags = extraTags
    });

    return new Dictionary<string, object?>
    {
        ["ResourceGroupName"] = rg.Name,
        ["VirtualNetworkName"] = vnet.VnetName,
        ["PublicSubnetId"] = vnet.PublicSubnetId,
        ["PrivateSubnetId"] = vnet.PrivateSubnetId,
        ["PrivateEndpointSubnetId"] = vnet.PrivateEndpointSubnetId
    };
});
