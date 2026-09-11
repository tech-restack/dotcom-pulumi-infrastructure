using Pulumi;

namespace Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup
{
    public class OrgResourceGroupArgs : ResourceArgs
    {
        [Input("environment", required: true)]
        public Input<string> Environment { get; set; } = null!;

        [Input("location")]
        public Input<string>? Location { get; set; }

        /// <summary>
        /// Cloud Adoption Framework tags from CafTags.ToInputMap().
        /// The library always overwrites env, region, and opsteam.
        /// </summary>
        [Input("extraTags")]
        public InputMap<string> ExtraTags { get; set; } = new();
    }
}
