using Pulumi;
using Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup;

namespace Dotcom.Cloud.Infrastructure.Azure.Compute.ContainerApp
{
    public class OrgContainerEnvironmentArgs : ResourceArgs
    {
        [Input("environment", required: true)]
        public Input<string> Environment { get; set; } = null!;

        [Input("resourceGroup", required: true)]
        public OrgResourceGroup ResourceGroup { get; set; } = null!;

        [Input("subnetId", required: true)]
        public Input<string> SubnetId { get; set; } = null!;

        /// <summary>
        /// When true (default), the environment FQDN is VNet-only.
        /// Set false for an internet-reachable environment.
        /// </summary>
        [Input("internal")]
        public Input<bool>? Internal { get; set; }

        /// <summary>
        /// Existing Log Analytics workspace ID. When omitted, the component creates one.
        /// </summary>
        [Input("logAnalyticsWorkspaceId")]
        public Input<string>? LogAnalyticsWorkspaceId { get; set; }

        [Input("logAnalyticsWorkspaceName")]
        public Input<string>? LogAnalyticsWorkspaceName { get; set; }

        [Input("extraTags")]
        public InputMap<string> ExtraTags { get; set; } = new();
    }
}
