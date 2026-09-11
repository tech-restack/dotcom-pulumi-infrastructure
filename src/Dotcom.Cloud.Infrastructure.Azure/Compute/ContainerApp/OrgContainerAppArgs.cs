using Pulumi;
using Dotcom.Cloud.Infrastructure.Azure.Containers.Registry;
using Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup;

namespace Dotcom.Cloud.Infrastructure.Azure.Compute.ContainerApp
{
    public class OrgContainerAppArgs : ResourceArgs
    {
        [Input("environment", required: true)]
        public Input<string> Environment { get; set; } = null!;

        [Input("resourceGroup", required: true)]
        public OrgResourceGroup ResourceGroup { get; set; } = null!;

        [Input("environmentId", required: true)]
        public Input<string> EnvironmentId { get; set; } = null!;

        [Input("image", required: true)]
        public Input<string> Image { get; set; } = null!;

        [Input("targetPort")]
        public Input<int>? TargetPort { get; set; }

        /// <summary>
        /// When true, Azure publishes an internet FQDN. Default false (VNet-only).
        /// The environment must also have Internal = false for public reachability.
        /// </summary>
        [Input("externalIngress")]
        public Input<bool>? ExternalIngress { get; set; }

        [Input("cpu")]
        public Input<double>? Cpu { get; set; }

        [Input("memory")]
        public Input<string>? Memory { get; set; }

        [Input("minReplicas")]
        public Input<int>? MinReplicas { get; set; }

        [Input("maxReplicas")]
        public Input<int>? MaxReplicas { get; set; }

        [Input("env")]
        public InputMap<string> Env { get; set; } = new();

        /// <summary>
        /// Optional org registry. When set, the app uses a user-assigned identity to pull.
        /// </summary>
        [Input("registry")]
        public OrgContainerRegistry? Registry { get; set; }

        /// <summary>
        /// When true (default), Pulumi creates AcrPull on the pull identity.
        /// Set false when CloudOps / Owner assigns AcrPull (Contributor cannot).
        /// </summary>
        public bool AssignAcrPull { get; set; } = true;

        [Input("extraTags")]
        public InputMap<string> ExtraTags { get; set; } = new();
    }
}
