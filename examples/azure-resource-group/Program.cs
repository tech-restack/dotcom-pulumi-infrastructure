using Pulumi;
using Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup;
using Dotcom.Cloud.Infrastructure.Common;

return await Deployment.RunAsync(() =>
{
    var config = new Config();
    string environment = config.Get("environment") ?? "dev";

    var extraTags = new CafTags
    {
        App = "example-resource-group",
        Tier = "infra",
        Criticality = "low",
        CostCenter = "poc",
        BusinessUnit = "shared"
    }.ToInputMap();

    var rg = new OrgResourceGroup("example-rg", new OrgResourceGroupArgs
    {
        Environment = environment,
        Location = "southafricanorth",
        ExtraTags = extraTags
    });

    return new Dictionary<string, object?>
    {
        ["ResourceGroupName"] = rg.Name,
        ["Location"] = rg.Location
    };
});
