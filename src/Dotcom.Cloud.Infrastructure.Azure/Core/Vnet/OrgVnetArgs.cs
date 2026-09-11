using Pulumi;
using Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup;

namespace Dotcom.Cloud.Infrastructure.Azure.Core.Vnet
{
    public class OrgVnetArgs : ResourceArgs
    {
        [Input("environment", required: true)]
        public Input<string> Environment { get; set; } = null!;

        [Input("resourceGroup", required: true)]
        public OrgResourceGroup ResourceGroup { get; set; } = null!;

        [Input("addressSpace", required: true)]
        public Input<string> AddressSpace { get; set; } = null!;

        [Input("publicSubnetCidr", required: true)]
        public Input<string> PublicSubnetCidr { get; set; } = null!;

        [Input("privateSubnetCidr", required: true)]
        public Input<string> PrivateSubnetCidr { get; set; } = null!;

        /// <summary>
        /// Optional subnet for Private Endpoints (ACR, Key Vault). Must not be the ACA infrastructure subnet.
        /// </summary>
        [Input("privateEndpointSubnetCidr")]
        public Input<string>? PrivateEndpointSubnetCidr { get; set; }

        /// <summary>
        /// Optional custom DNS servers. Omit to use Azure-provided DNS (required for Private DNS zones).
        /// </summary>
        [Input("dnsServers")]
        public InputList<string> DnsServers { get; set; } = new();

        [Input("location")]
        public Input<string>? Location { get; set; }

        [Input("extraTags")]
        public InputMap<string> ExtraTags { get; set; } = new();
    }
}
