using Pulumi;
using Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup;

namespace Dotcom.Cloud.Infrastructure.Azure.Containers.Registry
{
    public class OrgContainerRegistryArgs : ResourceArgs
    {
        [Input("environment", required: true)]
        public Input<string> Environment { get; set; } = null!;

        [Input("resourceGroup", required: true)]
        public OrgResourceGroup ResourceGroup { get; set; } = null!;

        /// <summary>
        /// Standard (default) or Premium. Private Endpoint path always uses Premium.
        /// </summary>
        [Input("sku")]
        public Input<string>? Sku { get; set; }

        [Input("privateEndpointSubnetId")]
        public Input<string>? PrivateEndpointSubnetId { get; set; }

        [Input("virtualNetworkId")]
        public Input<string>? VirtualNetworkId { get; set; }

        [Input("location")]
        public Input<string>? Location { get; set; }

        [Input("extraTags")]
        public InputMap<string> ExtraTags { get; set; } = new();
    }
}
