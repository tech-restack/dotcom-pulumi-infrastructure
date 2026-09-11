using Pulumi;
using AzureNative = Pulumi.AzureNative;
using Dotcom.Cloud.Infrastructure.Common;
using System.Collections.Generic;
using System.Linq;

namespace Dotcom.Cloud.Infrastructure.Azure.Core.Vnet
{
    public class OrgVnet : ComponentResource
    {
        [Output("vnetName")]
        public Output<string> VnetName { get; private set; } = null!;

        [Output("vnetId")]
        public Output<string> VnetId { get; private set; } = null!;

        [Output("publicSubnetId")]
        public Output<string> PublicSubnetId { get; private set; } = null!;

        [Output("privateSubnetId")]
        public Output<string> PrivateSubnetId { get; private set; } = null!;

        [Output("privateEndpointSubnetId")]
        public Output<string> PrivateEndpointSubnetId { get; private set; } = null!;

        public OrgVnet(string name, OrgVnetArgs args, ComponentResourceOptions? options = null)
            : base("dotcom:core:OrgVnet", name, args, options)
        {
            var location = args.Location ?? args.ResourceGroup.Location;
            var vnetName = args.Environment.Apply(env => NamingConventions.GetResourceName(name, env, "vnet"));
            var tags = GlobalTags.Resolve(args.Environment, location, args.ExtraTags);
            var customResourceOptions = new CustomResourceOptions { Parent = this };

            var vnet = new AzureNative.Network.VirtualNetwork(name, new()
            {
                ResourceGroupName = args.ResourceGroup.Name,
                VirtualNetworkName = vnetName,
                Location = location,
                AddressSpace = new AzureNative.Network.Inputs.AddressSpaceArgs
                {
                    AddressPrefixes = { args.AddressSpace }
                },
                Tags = tags
            }, customResourceOptions);

            var publicNsg = new AzureNative.Network.NetworkSecurityGroup($"{name}-public-nsg", new()
            {
                ResourceGroupName = args.ResourceGroup.Name,
                Location = location,
                SecurityRules = OrgNetworkBaselines.PublicSubnetInbound()
                    .Concat(OrgNetworkBaselines.PublicSubnetOutbound())
                    .ToArray(),
                Tags = tags
            }, customResourceOptions);

            var privateNsg = new AzureNative.Network.NetworkSecurityGroup($"{name}-private-nsg", new()
            {
                ResourceGroupName = args.ResourceGroup.Name,
                Location = location,
                SecurityRules = OrgNetworkBaselines.PrivateSubnetInbound()
                    .Concat(OrgNetworkBaselines.PrivateSubnetOutbound())
                    .ToArray(),
                Tags = tags
            }, customResourceOptions);

            // Subnets are separate resources (not inline on the VNet). Azure can
            // return 403 on subnet GET for a few seconds after the VNet is created
            // instead of 404. Explicit DependsOn waits until the VNet and NSG exist.
            var publicSubnet = new AzureNative.Network.Subnet($"{name}-public", new()
            {
                ResourceGroupName = args.ResourceGroup.Name,
                VirtualNetworkName = vnet.Name,
                SubnetName = "public",
                AddressPrefix = args.PublicSubnetCidr,
                NetworkSecurityGroup = new AzureNative.Network.Inputs.NetworkSecurityGroupArgs { Id = publicNsg.Id }
            }, new CustomResourceOptions { Parent = this, DependsOn = { vnet, publicNsg } });

            var privateSubnet = new AzureNative.Network.Subnet($"{name}-private", new()
            {
                ResourceGroupName = args.ResourceGroup.Name,
                VirtualNetworkName = vnet.Name,
                SubnetName = "private",
                AddressPrefix = args.PrivateSubnetCidr,
                NetworkSecurityGroup = new AzureNative.Network.Inputs.NetworkSecurityGroupArgs { Id = privateNsg.Id },
                Delegations =
                {
                    new AzureNative.Network.Inputs.DelegationArgs
                    {
                        Name = "MicrosoftAppEnvironments",
                        ServiceName = "Microsoft.App/environments"
                    }
                }
            }, new CustomResourceOptions { Parent = this, DependsOn = { vnet, privateNsg } });

            this.VnetName = vnet.Name;
            this.VnetId = vnet.Id;
            this.PublicSubnetId = publicSubnet.Id;
            this.PrivateSubnetId = privateSubnet.Id;

            if (args.PrivateEndpointSubnetCidr != null)
            {
                var peNsg = new AzureNative.Network.NetworkSecurityGroup($"{name}-pe-nsg", new()
                {
                    ResourceGroupName = args.ResourceGroup.Name,
                    Location = location,
                    SecurityRules = OrgNetworkBaselines.PrivateEndpointSubnetInbound()
                        .Concat(OrgNetworkBaselines.PrivateEndpointSubnetOutbound())
                        .ToArray(),
                    Tags = tags
                }, customResourceOptions);

                var peSubnet = new AzureNative.Network.Subnet($"{name}-pe", new()
                {
                    ResourceGroupName = args.ResourceGroup.Name,
                    VirtualNetworkName = vnet.Name,
                    SubnetName = "privatelink",
                    AddressPrefix = args.PrivateEndpointSubnetCidr,
                    NetworkSecurityGroup = new AzureNative.Network.Inputs.NetworkSecurityGroupArgs { Id = peNsg.Id },
                    PrivateEndpointNetworkPolicies = AzureNative.Network.VirtualNetworkPrivateEndpointNetworkPolicies.Disabled
                }, new CustomResourceOptions { Parent = this, DependsOn = { vnet, peNsg } });

                this.PrivateEndpointSubnetId = peSubnet.Id;
            }

            this.RegisterOutputs(new Dictionary<string, object?>
            {
                { "vnetName", this.VnetName },
                { "vnetId", this.VnetId },
                { "publicSubnetId", this.PublicSubnetId },
                { "privateSubnetId", this.PrivateSubnetId },
                { "privateEndpointSubnetId", this.PrivateEndpointSubnetId }
            });
        }
    }
}
