using Pulumi;
using AzureNative = Pulumi.AzureNative;
using Dotcom.Cloud.Infrastructure.Common;
using System.Collections.Generic;

namespace Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup
{
    public class OrgResourceGroup : ComponentResource
    {
        [Output("name")]
        public Output<string> Name { get; private set; } = null!;

        [Output("location")]
        public Output<string> Location { get; private set; } = null!;

        public OrgResourceGroup(string name, OrgResourceGroupArgs args, ComponentResourceOptions? options = null)
            : base("dotcom:core:OrgResourceGroup", name, args, options)
        {
            var location = args.Location ?? "southafricanorth";
            var rgName = args.Environment.Apply(env => NamingConventions.GetResourceName(name, env, "rg"));
            var tags = GlobalTags.Resolve(args.Environment, location, args.ExtraTags);

            var rg = new AzureNative.Resources.ResourceGroup(name, new()
            {
                ResourceGroupName = rgName,
                Location = location,
                Tags = tags
            }, new CustomResourceOptions { Parent = this });

            this.Name = rg.Name;
            this.Location = rg.Location;

            this.RegisterOutputs(new Dictionary<string, object?>
            {
                { "name", this.Name },
                { "location", this.Location }
            });
        }
    }
}
