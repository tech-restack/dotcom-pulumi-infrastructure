# Examples

Runnable Pulumi programs that compose `Org*` components. They sit **next to** `src/` and `tests/`, not inside the Azure project, so they are not packed into NuGet.

| Path | Component | What it shows |
| --- | --- | --- |
| [`azure-resource-group/`](azure-resource-group/) | `OrgResourceGroup` | CAF tags on a resource group |
| [`azure-vnet/`](azure-vnet/) | `OrgVnet` | Public, ACA-delegated private, and private-endpoint subnets |
| [`azure-container-registry/`](azure-container-registry/) | `OrgContainerRegistry` | Private Premium ACR + private endpoint |
| [`azure-container-app/`](azure-container-app/) | `OrgContainerEnvironment`, `OrgContainerApp` | VNet-injected environment and app (MCR image, no public ingress) |
| [`azure-linux-vm/`](azure-linux-vm/) | `OrgVirtualMachine` | Private Linux VM (SSH key, no public IP) |
| [`azure-app-service/`](azure-app-service/) | `OrgAppService` | Not implemented |
| [`aws/`](aws/) | AWS library | Placeholder |
| [`gcp/`](gcp/) | GCP library | Placeholder |

**Library authors:** `ProjectReference` the Azure project so the example tracks local source. `dotnet build` from the repo root includes these programs.

**Workload teams:** copy the `Program.cs` pattern into your stack, then restore `Dotcom.Cloud.Infrastructure.Azure` from GitHub Packages instead of a project reference.

Do not `pulumi up` these samples against a shared production subscription. Preview against a sandbox, or read the code only.
