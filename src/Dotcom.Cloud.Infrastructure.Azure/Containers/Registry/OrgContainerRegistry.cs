using Pulumi;
using AzureNative = Pulumi.AzureNative;
using Dotcom.Cloud.Infrastructure.Common;
using System.Collections.Generic;

namespace Dotcom.Cloud.Infrastructure.Azure.Containers.Registry
{
    public class OrgContainerRegistry : ComponentResource
    {
        [Output("id")]
        public Output<string> Id { get; private set; } = null!;

        [Output("name")]
        public Output<string> Name { get; private set; } = null!;

        [Output("loginServer")]
        public Output<string> LoginServer { get; private set; } = null!;

        public OrgContainerRegistry(string name, OrgContainerRegistryArgs args, ComponentResourceOptions? options = null)
            : base("dotcom:containers:OrgContainerRegistry", name, args, options)
        {
            var location = args.Location ?? args.ResourceGroup.Location;
            var tags = GlobalTags.Resolve(args.Environment, location, args.ExtraTags);
            var usePrivateEndpoint = args.PrivateEndpointSubnetId != null && args.VirtualNetworkId != null;
            var skuName = usePrivateEndpoint
                ? "Premium"
                : args.Sku ?? "Standard";
            var opts = new CustomResourceOptions { Parent = this };

            var registry = new AzureNative.ContainerRegistry.Registry(name, new()
            {
                ResourceGroupName = args.ResourceGroup.Name,
                RegistryName = args.Environment.Apply(e => NamingConventions.GetAcrName(name, e)),
                Location = location,
                Sku = new AzureNative.ContainerRegistry.Inputs.SkuArgs
                {
                    Name = skuName
                },
                AdminUserEnabled = false,
                AnonymousPullEnabled = false,
                PublicNetworkAccess = usePrivateEndpoint
                    ? AzureNative.ContainerRegistry.PublicNetworkAccess.Disabled
                    : AzureNative.ContainerRegistry.PublicNetworkAccess.Enabled,
                NetworkRuleBypassOptions = AzureNative.ContainerRegistry.NetworkRuleBypassOptions.AzureServices,
                Tags = tags
            }, opts);

            this.Id = registry.Id;
            this.Name = registry.Name;
            this.LoginServer = registry.LoginServer;

            if (usePrivateEndpoint)
            {
                var zone = new AzureNative.PrivateDns.PrivateZone($"{name}-acr-pdz", new()
                {
                    ResourceGroupName = args.ResourceGroup.Name,
                    PrivateZoneName = "privatelink.azurecr.io",
                    Location = "global"
                }, opts);

                _ = new AzureNative.PrivateDns.VirtualNetworkLink($"{name}-acr-pdz-link", new()
                {
                    ResourceGroupName = args.ResourceGroup.Name,
                    PrivateZoneName = zone.Name,
                    Location = "global",
                    RegistrationEnabled = false,
                    VirtualNetwork = new AzureNative.PrivateDns.Inputs.SubResourceArgs
                    {
                        Id = args.VirtualNetworkId
                    }
                }, opts);

                var privateEndpoint = new AzureNative.Network.PrivateEndpoint($"{name}-acr-pe", new()
                {
                    ResourceGroupName = args.ResourceGroup.Name,
                    Location = location,
                    Subnet = new AzureNative.Network.Inputs.SubnetArgs
                    {
                        Id = args.PrivateEndpointSubnetId
                    },
                    PrivateLinkServiceConnections =
                    {
                        new AzureNative.Network.Inputs.PrivateLinkServiceConnectionArgs
                        {
                            Name = $"{name}-acr",
                            PrivateLinkServiceId = registry.Id,
                            GroupIds = { "registry" }
                        }
                    },
                    Tags = tags
                }, opts);

                _ = new AzureNative.Network.PrivateDnsZoneGroup($"{name}-acr-pdzg", new()
                {
                    ResourceGroupName = args.ResourceGroup.Name,
                    PrivateEndpointName = privateEndpoint.Name,
                    PrivateDnsZoneConfigs =
                    {
                        new AzureNative.Network.Inputs.PrivateDnsZoneConfigArgs
                        {
                            Name = "privatelink-azurecr-io",
                            PrivateDnsZoneId = zone.Id
                        }
                    }
                }, opts);
            }

            this.RegisterOutputs(new Dictionary<string, object?>
            {
                { "id", this.Id },
                { "name", this.Name },
                { "loginServer", this.LoginServer }
            });
        }
    }
}
