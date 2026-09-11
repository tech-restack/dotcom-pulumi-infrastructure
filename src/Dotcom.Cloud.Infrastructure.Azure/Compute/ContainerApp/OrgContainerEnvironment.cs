using Pulumi;
using AzureNative = Pulumi.AzureNative;
using Dotcom.Cloud.Infrastructure.Common;
using System.Collections.Generic;

namespace Dotcom.Cloud.Infrastructure.Azure.Compute.ContainerApp
{
    public class OrgContainerEnvironment : ComponentResource
    {
        [Output("environmentId")]
        public Output<string> EnvironmentId { get; private set; } = null!;

        [Output("logAnalyticsWorkspaceId")]
        public Output<string> LogAnalyticsWorkspaceId { get; private set; } = null!;

        public OrgContainerEnvironment(string name, OrgContainerEnvironmentArgs args, ComponentResourceOptions? options = null)
            : base("dotcom:compute:OrgContainerEnvironment", name, args, options)
        {
            var tags = GlobalTags.Resolve(args.Environment, args.ResourceGroup.Location, args.ExtraTags);
            var internalEnv = args.Internal ?? true;
            var opts = new CustomResourceOptions { Parent = this };

            Output<string> workspaceId;
            Output<string> workspaceCustomerId;
            Output<string> workspaceKey;

            if (args.LogAnalyticsWorkspaceId != null && args.LogAnalyticsWorkspaceName != null)
            {
                workspaceId = args.LogAnalyticsWorkspaceId.ToOutput();
                var existingKeys = AzureNative.OperationalInsights.GetSharedKeys.Invoke(new()
                {
                    ResourceGroupName = args.ResourceGroup.Name,
                    WorkspaceName = args.LogAnalyticsWorkspaceName
                });
                workspaceCustomerId = AzureNative.OperationalInsights.GetWorkspace.Invoke(new()
                {
                    ResourceGroupName = args.ResourceGroup.Name,
                    WorkspaceName = args.LogAnalyticsWorkspaceName
                }).Apply(w => w.CustomerId ?? string.Empty);
                workspaceKey = existingKeys.Apply(k => k.PrimarySharedKey ?? string.Empty);
            }
            else
            {
                var workspace = new AzureNative.OperationalInsights.Workspace($"{name}-law", new()
                {
                    ResourceGroupName = args.ResourceGroup.Name,
                    WorkspaceName = args.Environment.Apply(e => NamingConventions.GetResourceName(name, e, "law")),
                    Location = args.ResourceGroup.Location,
                    Sku = new AzureNative.OperationalInsights.Inputs.WorkspaceSkuArgs
                    {
                        Name = "PerGB2018"
                    },
                    RetentionInDays = 30,
                    Tags = tags
                }, opts);

                var keys = AzureNative.OperationalInsights.GetSharedKeys.Invoke(new()
                {
                    ResourceGroupName = args.ResourceGroup.Name,
                    WorkspaceName = workspace.Name
                });

                workspaceId = workspace.Id;
                workspaceCustomerId = workspace.CustomerId;
                workspaceKey = keys.Apply(k => k.PrimarySharedKey ?? string.Empty);
            }

            var managedEnv = new AzureNative.App.ManagedEnvironment(name, new()
            {
                ResourceGroupName = args.ResourceGroup.Name,
                EnvironmentName = args.Environment.Apply(e => NamingConventions.GetResourceName(name, e, "cae")),
                Location = args.ResourceGroup.Location,
                VnetConfiguration = new AzureNative.App.Inputs.VnetConfigurationArgs
                {
                    InfrastructureSubnetId = args.SubnetId,
                    Internal = internalEnv
                },
                AppLogsConfiguration = new AzureNative.App.Inputs.AppLogsConfigurationArgs
                {
                    Destination = "log-analytics",
                    LogAnalyticsConfiguration = new AzureNative.App.Inputs.LogAnalyticsConfigurationArgs
                    {
                        CustomerId = workspaceCustomerId,
                        SharedKey = workspaceKey
                    }
                },
                WorkloadProfiles =
                {
                    new AzureNative.App.Inputs.WorkloadProfileArgs
                    {
                        Name = "Consumption",
                        WorkloadProfileType = "Consumption"
                    }
                },
                Tags = tags
            }, opts);

            this.EnvironmentId = managedEnv.Id;
            this.LogAnalyticsWorkspaceId = workspaceId;

            this.RegisterOutputs(new Dictionary<string, object?>
            {
                { "environmentId", this.EnvironmentId },
                { "logAnalyticsWorkspaceId", this.LogAnalyticsWorkspaceId }
            });
        }
    }
}
