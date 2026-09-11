using Pulumi;
using AzureNative = Pulumi.AzureNative;
using Dotcom.Cloud.Infrastructure.Common;
using System.Collections.Generic;
using System.Linq;

namespace Dotcom.Cloud.Infrastructure.Azure.Compute.ContainerApp
{
    public class OrgContainerApp : ComponentResource
    {
        private const string AcrPullRoleId = "7f951dda-4ed3-4680-a7ca-43fe172d538d";

        [Output("fqdn")]
        public Output<string> Fqdn { get; private set; } = null!;

        [Output("principalId")]
        public Output<string> PrincipalId { get; private set; } = null!;

        public OrgContainerApp(string name, OrgContainerAppArgs args, ComponentResourceOptions? options = null)
            : base("dotcom:compute:OrgContainerApp", name, args, options)
        {
            var tags = GlobalTags.Resolve(args.Environment, args.ResourceGroup.Location, args.ExtraTags);
            var port = args.TargetPort ?? 80;
            var cpu = args.Cpu ?? 0.5;
            var memory = args.Memory ?? "1.0Gi";
            var minReplicas = args.MinReplicas ?? 0;
            var maxReplicas = args.MaxReplicas ?? 5;
            var external = args.ExternalIngress ?? false;
            var opts = new CustomResourceOptions { Parent = this };

            var envVars = args.Env.ToOutput().Apply(map =>
                map.Select(kv => new AzureNative.App.Inputs.EnvironmentVarArgs
                {
                    Name = kv.Key,
                    Value = kv.Value
                }).ToArray());

            var scale = new AzureNative.App.Inputs.ScaleArgs
            {
                MinReplicas = minReplicas,
                MaxReplicas = maxReplicas
            };

            var configuration = new AzureNative.App.Inputs.ConfigurationArgs
            {
                Ingress = new AzureNative.App.Inputs.IngressArgs
                {
                    External = external,
                    TargetPort = port,
                    Transport = AzureNative.App.IngressTransportMethod.Auto,
                    AllowInsecure = false
                }
            };

            var identity = new AzureNative.App.Inputs.ManagedServiceIdentityArgs
            {
                Type = AzureNative.App.ManagedServiceIdentityType.SystemAssigned
            };

            var appDependsOn = new InputList<Resource>();

            if (args.Registry != null)
            {
                // User-assigned identity is created *before* the revision so AcrPull
                // exists when Azure tries to pull. System-assigned identity cannot do
                // that: the app must exist before it has a principal.
                var pullIdentity = new AzureNative.ManagedIdentity.UserAssignedIdentity($"{name}-acr-id", new()
                {
                    ResourceGroupName = args.ResourceGroup.Name,
                    Location = args.ResourceGroup.Location,
                    ResourceName = args.Environment.Apply(e => NamingConventions.GetResourceName(name, e, "id")),
                    Tags = tags
                }, opts);

                var assignPull = args.AssignAcrPull;
                if (assignPull)
                {
                    var clientConfig = AzureNative.Authorization.GetClientConfig.Invoke();
                    var acrPull = new AzureNative.Authorization.RoleAssignment($"{name}-acrpull", new()
                    {
                        PrincipalId = pullIdentity.PrincipalId,
                        PrincipalType = AzureNative.Authorization.PrincipalType.ServicePrincipal,
                        RoleDefinitionId = clientConfig.Apply(c =>
                            $"/subscriptions/{c.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{AcrPullRoleId}"),
                        Scope = args.Registry.Id
                    }, opts);
                    appDependsOn.Add(acrPull);
                }

                configuration.Registries =
                [
                    new AzureNative.App.Inputs.RegistryCredentialsArgs
                    {
                        Server = args.Registry.LoginServer,
                        Identity = pullIdentity.Id
                    }
                ];

                identity = new AzureNative.App.Inputs.ManagedServiceIdentityArgs
                {
                    Type = AzureNative.App.ManagedServiceIdentityType.SystemAssigned_UserAssigned,
                    UserAssignedIdentities = { pullIdentity.Id }
                };

                this.PrincipalId = pullIdentity.PrincipalId;
            }

            var appOpts = new CustomResourceOptions
            {
                Parent = this,
                DependsOn = appDependsOn
            };

            var containerApp = new AzureNative.App.ContainerApp(name, new()
            {
                ResourceGroupName = args.ResourceGroup.Name,
                ContainerAppName = args.Environment.Apply(e => NamingConventions.GetResourceName(name, e, "ca")),
                ManagedEnvironmentId = args.EnvironmentId,
                WorkloadProfileName = "Consumption",
                Identity = identity,
                Configuration = configuration,
                Template = new AzureNative.App.Inputs.TemplateArgs
                {
                    Containers =
                    {
                        new AzureNative.App.Inputs.ContainerArgs
                        {
                            Name = "app-main",
                            Image = args.Image,
                            Env = envVars,
                            Resources = new AzureNative.App.Inputs.ContainerResourcesArgs
                            {
                                Cpu = cpu,
                                Memory = memory
                            }
                        }
                    },
                    Scale = scale
                },
                Tags = tags
            }, appOpts);

            this.Fqdn = containerApp.Configuration.Apply(c => c?.Ingress?.Fqdn ?? string.Empty);
            if (args.Registry == null)
            {
                this.PrincipalId = containerApp.Identity.Apply(i => i?.PrincipalId ?? string.Empty);
            }

            this.RegisterOutputs(new Dictionary<string, object?>
            {
                { "fqdn", this.Fqdn },
                { "principalId", this.PrincipalId }
            });
        }
    }
}
