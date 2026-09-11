using Pulumi;
using Dotcom.Cloud.Infrastructure.Azure.Compute.VirtualMachine;
using Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup;
using Dotcom.Cloud.Infrastructure.Azure.Core.Vnet;
using Dotcom.Cloud.Infrastructure.Common;

return await Deployment.RunAsync(() =>
{
    var config = new Config();
    string environment = config.Get("environment") ?? "dev";

    var extraTags = new CafTags
    {
        App = "example-linux-vm",
        Tier = "web",
        Criticality = "low",
        CostCenter = "poc",
        BusinessUnit = "shared"
    }.ToInputMap();

    var rg = new OrgResourceGroup("example-linux-vm", new OrgResourceGroupArgs
    {
        Environment = environment,
        Location = "southafricanorth",
        ExtraTags = extraTags
    });

    var vnet = new OrgVnet("example-network", new OrgVnetArgs
    {
        Environment = environment,
        ResourceGroup = rg,
        AddressSpace = "10.40.0.0/16",
        PublicSubnetCidr = "10.40.1.0/24",
        PrivateSubnetCidr = "10.40.2.0/24",
        ExtraTags = extraTags
    });

    // PrivateSubnetId is delegated to Container Apps — VMs must not use it.
    var vm = new OrgVirtualMachine("example-linux-vm", new OrgVirtualMachineArgs
    {
        Environment = environment,
        ResourceGroup = rg,
        SubnetId = vnet.PublicSubnetId,
        OsType = OrgVmOsType.Linux,
        SshPublicKey = config.Require("sshPublicKey"),
        ExtraTags = extraTags
    });

    return new Dictionary<string, object?>
    {
        ["ResourceGroupName"] = rg.Name,
        ["VmName"] = vm.Name,
        ["VmPrivateIp"] = vm.PrivateIpAddress,
        ["VmPrincipalId"] = vm.PrincipalId
    };
});
