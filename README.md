# Dotcom Cloud Infrastructure

Enterprise Pulumi component library for standardized, paved-road infrastructure across Azure, AWS, and Google Cloud.

**Pipeline & Quality:**  
[![Build](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/tech-restack/dotcom-pulumi-infrastructure/actions/workflows/publish-nuget.yml)
[![Coverage](https://img.shields.io/badge/coverage-placeholder-lightgrey)](#testing)
[![Security Scan](https://img.shields.io/badge/security%20scan-planned-lightgrey)](#security--cost-governance)

**Release & Stack:**  
[![NuGet](https://img.shields.io/badge/nuget-2.2.1-blue)](https://github.com/orgs/tech-restack/packages?repo_name=dotcom-pulumi-infrastructure)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Pulumi](https://img.shields.io/badge/Pulumi-IaC-8a3391)](https://www.pulumi.com/)

**Governance:**  
[![Dependabot](https://img.shields.io/badge/dependabot-planned-lightgrey)](#security--cost-governance)
[![Infracost](https://img.shields.io/badge/Infracost-planned-lightgrey)](#security--cost-governance)
[![License: Proprietary](https://img.shields.io/badge/License-Internal_Proprietary-red.svg)](#)

---

## Overview

This repository is the **org-wide Pulumi paved road**. Workload teams do not hand-author Azure Resource Manager, AWS, or GCP primitives for common platform building blocks. They compose `Org*` components that already encode:

- Resource naming (`rg-{env}-{name}`, ACR alphanumeric constraints)
- Microsoft Cloud Adoption Framework (CAF) tagging
- Network baselines (NSGs, subnet purpose, Container Apps delegation)
- Private registry defaults (Premium ACR, public access off, private endpoint)
- Container Apps defaults (identities, ingress posture, registry pull)

The library is written in **C# on .NET 10.0** and published internally as **NuGet packages** to GitHub Packages (`tech-restack`). This repository is the **component catalog**, not a Pulumi backend. Workload stacks store state in the object store of the cloud they deploy to:

| Cloud | Pulumi state backend |
| --- | --- |
| AWS | Amazon S3 |
| GCP | Google Cloud Storage |
| Azure | Azure Blob Storage |
| Any cloud (shared DB) | PostgreSQL |

Local development and isolated tests may use the **local file backend** or **Pulumi Cloud**. Do not treat Pulumi Cloud (or any state backend) as the place teams consume `OrgResourceGroup`; that remains GitHub Packages. Create the store **before** the first `pulumi up` — see [DIY state backend bootstrap](#diy-state-backend-bootstrap).

**NuGet packages**

| Package | Status | Distributed |
| --- | --- | --- |
| `Dotcom.Cloud.Infrastructure.Common` | Naming, CAF tags, org-owned tag merge | Yes (`2.2.1`) |
| `Dotcom.Cloud.Infrastructure.Azure` | Resource Group, VNet, ACR, Container Apps | Yes (`2.2.1`) |
| `Dotcom.Cloud.Infrastructure.Aws` | Placeholder | No (`IsPackable=false`) |
| `Dotcom.Cloud.Infrastructure.Gcp` | Placeholder | No (`IsPackable=false`) |

**Design rules for callers**

1. Keep workload programs thin. Instantiate `Org*` types; do not pass raw ARM dictionaries through this library.
2. Org tags always win. `env`, `region`, and `opsteam` cannot be overridden by the workload.
3. Unknown or legacy tag keys (`application`, `environment`, `owner`) fail at `pulumi preview`.
4. Pulumi type tokens (`dotcom:core:OrgResourceGroup`, `dotcom:core:OrgVnet`, …) are stable. Renaming them replaces live stack resources.

---

## Repository Structure

```
dotcom-pulumi-infrastructure/
├── .github/workflows/publish-nuget.yml   # restore, test, pack, push to GitHub Packages
├── Directory.Build.props                 # net10.0, Version, shared package metadata
├── Dotcom.Cloud.slnx                     # solution
├── nuget.config.example                  # copy into workload repos (sources only)
├── src/
│   ├── Dotcom.Cloud.Infrastructure.Common/
│   │   ├── CafTags.cs                    # workload CAF tag contract
│   │   ├── GlobalTags.cs                 # merge + validation; org tags win
│   │   └── NamingConventions.cs          # rg-/vnet-/cae- names and ACR rules
│   ├── Dotcom.Cloud.Infrastructure.Azure/
│   │   ├── Core/ResourceGroup/           # OrgResourceGroup
│   │   ├── Core/Vnet/                    # OrgVnet, NSG baselines
│   │   ├── Containers/Registry/          # OrgContainerRegistry (private Premium ACR)
│   │   ├── Compute/ContainerApp/         # OrgContainerEnvironment, OrgContainerApp
│   │   └── Web/AppService/               # stub until a workload requires App Service
│   ├── Dotcom.Cloud.Infrastructure.Aws/  # not implemented; not packed
│   └── Dotcom.Cloud.Infrastructure.Gcp/  # not implemented; not packed
└── tests/Dotcom.Cloud.Tests/             # xUnit: naming and CAF tag behaviour
```

`nupkgs/` is the local pack output directory. It is gitignored. CI publishes Common and Azure only.

### Azure components

| Component | Pulumi token | What it creates |
| --- | --- | --- |
| `OrgResourceGroup` | `dotcom:core:OrgResourceGroup` | Named, CAF-tagged resource group |
| `OrgVnet` | `dotcom:core:OrgVnet` | VNet, public / private / optional PE subnets, NSGs |
| `OrgContainerRegistry` | `dotcom:containers:OrgContainerRegistry` | Private Premium ACR + private endpoint + DNS |
| `OrgContainerEnvironment` | `dotcom:compute:OrgContainerEnvironment` | Log Analytics + Container Apps environment |
| `OrgContainerApp` | `dotcom:compute:OrgContainerApp` | App, optional UAI + AcrPull for private pulls |
| `OrgAppService` | — | Unimplemented placeholder |

---

## Prerequisites

### Consume the library (workload teams)

| Requirement | Notes |
| --- | --- |
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Matches `TargetFramework` in `Directory.Build.props` |
| [Pulumi CLI](https://www.pulumi.com/docs/install/) 3.x | `pulumi preview` / `pulumi up` |
| Cloud credentials | **AWS:** `aws sso login` (or `aws configure`) and an account the stack may write to. **Azure:** `az login` and a subscription the stack may write to. **GCP:** `gcloud auth application-default login` and a project the stack may write to. |
| GitHub Packages access | Classic PAT with `read:packages`. After creating the token, **Enable SSO** and **Authorize** it for `tech-restack`. |
| `nuget.config` | GitHub Packages feed + nuget.org; **never** commit the PAT |

### Contribute to this repository

Everything above, plus:

- Ability to push a branch and open a pull request against `tech-restack/dotcom-pulumi-infrastructure`
- `write:packages` is **not** required on a developer PAT; `main` publishes via `GITHUB_TOKEN`

Default Azure location in components is `southafricanorth` when `Location` is omitted.

---

## DIY state backend bootstrap

Cloud Operations creates the state store with the **cloud CLI**, then workload teams `pulumi login` to it. Do **not** create the bucket, container, or database in the same Pulumi stack that will use it as a backend.

DIY backends store JSON checkpoints. They do **not** encrypt secrets the way Pulumi Cloud does. Every stack must set a secrets provider at init time.

```bash
# Laptop / isolated test
pulumi stack init dev --secrets-provider=passphrase

# Match the cloud (preferred for shared stacks)
pulumi stack init prod --secrets-provider="awskms://alias/pulumi-secrets?region=eu-west-1"
pulumi stack init prod --secrets-provider="azurekeyvault://<vault>.vault.azure.net/keys/<key>"
pulumi stack init prod --secrets-provider="gcpkms://projects/<project>/locations/<loc>/keyRings/<ring>/cryptoKeys/<key>"
```

### AWS — S3

```bash
aws s3api create-bucket \
  --bucket org-pulumi-state-prod \
  --region eu-west-1 \
  --create-bucket-configuration LocationConstraint=eu-west-1

aws s3api put-bucket-versioning \
  --bucket org-pulumi-state-prod \
  --versioning-configuration Status=Enabled

aws s3api put-bucket-encryption \
  --bucket org-pulumi-state-prod \
  --server-side-encryption-configuration \
  '{"Rules":[{"ApplyServerSideEncryptionByDefault":{"SSEAlgorithm":"aws:kms"}}]}'

aws s3api put-public-access-block \
  --bucket org-pulumi-state-prod \
  --public-access-block-configuration \
  BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true

export AWS_PROFILE=cloudops
pulumi login 's3://org-pulumi-state-prod?region=eu-west-1&awssdk=v2&profile=cloudops'
```

The calling identity needs `s3:ListBucket` on the bucket ARN, and `s3:GetObject` / `PutObject` / `DeleteObject` on `arn:aws:s3:::org-pulumi-state-prod/*`.

### Azure — Blob Storage

```bash
az group create -n rg-prod-pulumi-state -l southafricanorth

az storage account create \
  -n stpulumistateprod \
  -g rg-prod-pulumi-state \
  -l southafricanorth \
  --sku Standard_ZRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false

az storage container create \
  --account-name stpulumistateprod \
  --name pulumi \
  --auth-mode login

export AZURE_STORAGE_ACCOUNT=stpulumistateprod
az login
pulumi login 'azblob://pulumi?storage_account=stpulumistateprod'
```

The identity that runs Pulumi needs **Storage Blob Data Contributor** on the account or container. Backend auth uses `AZURE_*` (or `az login`), not `ARM_*`. Prefer Microsoft Entra over `AZURE_STORAGE_KEY` in CI.

### GCP — Cloud Storage

```bash
gcloud storage buckets create gs://org-pulumi-state-prod \
  --location=EUROPE-WEST1 \
  --uniform-bucket-level-access \
  --public-access-prevention

gcloud storage buckets update gs://org-pulumi-state-prod --versioning

gcloud auth application-default login
pulumi login gs://org-pulumi-state-prod
```

### PostgreSQL (shared / multi-cloud)

```sql
CREATE DATABASE pulumi_state;
CREATE ROLE pulumi_backend LOGIN PASSWORD '...';
GRANT ALL ON DATABASE pulumi_state TO pulumi_backend;
```

```bash
export PGHOST=pulumi-state.example.internal
export PGPORT=5432
export PGUSER=pulumi_backend
export PGPASSWORD='...'          # do not put this in the login URL
export PGDATABASE=pulumi_state

pulumi login 'postgres://?sslmode=require'
```

Pulumi creates table `pulumi_state` by default. Override with `?table=<name>` if required.

### Local testing

```bash
pulumi login --local                    # file://~ → ~/.pulumi
pulumi login file:///tmp/pulumi-state   # explicit directory
pulumi login                            # Pulumi Cloud, laptop PoC only
```

### Workload login

From the stack directory, log into the backend that matches the cloud, then preview:

```bash
cd /path/to/workload
pulumi login 'azblob://pulumi?storage_account=stpulumistateprod'
pulumi stack init dev --secrets-provider=passphrase
pulumi preview
```

State files live under `.pulumi/stacks/` in that backend. Locking is file-based on object storage. Cloud Operations owns backups, versioning, and who may `List`/`Get`/`Put`/`Delete`.

---

## Getting Started / Usage

### 1. Add the GitHub Packages feed

Copy [`nuget.config.example`](nuget.config.example) into the workload repo as `nuget.config`. Do not put credentials in that file.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/tech-restack/index.json" />
  </packageSources>
</configuration>
```

Authenticate once on the developer machine (user NuGet config, not the repo file):

```bash
export GITHUB_TOKEN=ghp_...   # classic PAT, read:packages
dotnet nuget update source github \
  --username YOUR_GITHUB_USERNAME \
  --password "$GITHUB_TOKEN" \
  --store-password-in-clear-text
```

`tech-restack` uses SAML SSO. After creating the PAT: GitHub → Settings → Developer settings → Personal access tokens → **Enable SSO** → **Authorize** for `tech-restack`. Restore fails with `Forbidden` until that authorization is done.

### 2. Reference the Azure package

```xml
<ItemGroup>
  <PackageReference Include="Dotcom.Cloud.Infrastructure.Azure" Version="2.2.1" />
</ItemGroup>
```

`Dotcom.Cloud.Infrastructure.Common` is pulled transitively. Pin the version; do not use floating versions for org components.

### 3. Compose a resource group and VNet

Workload programs stay in `Pulumi.Deployment.RunAsync`. Pass `Output`s between components; do not block on `.Result`.

```csharp
using System.Collections.Generic;
using Pulumi;
using Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup;
using Dotcom.Cloud.Infrastructure.Azure.Core.Vnet;
using Dotcom.Cloud.Infrastructure.Common;

return await Deployment.RunAsync(() =>
{
    var extraTags = new CafTags
    {
        App = "payments-api",
        Tier = "api",
        Criticality = "medium",
        CostCenter = "cc-1042",
        BusinessUnit = "retail"
    }.ToInputMap();

    var rg = new OrgResourceGroup("payments-api", new OrgResourceGroupArgs
    {
        Environment = "dev",
        Location = "southafricanorth",
        ExtraTags = extraTags
    });

    var vnet = new OrgVnet("app-network", new OrgVnetArgs
    {
        Environment = "dev",
        ResourceGroup = rg,
        AddressSpace = "10.0.0.0/16",
        PublicSubnetCidr = "10.0.1.0/24",
        PrivateSubnetCidr = "10.0.2.0/24",
        PrivateEndpointSubnetCidr = "10.0.3.0/24",
        ExtraTags = extraTags
    });

    return new Dictionary<string, object?>
    {
        ["ResourceGroupName"] = rg.Name,
        ["VirtualNetworkName"] = vnet.VnetName
    };
});
```

Azure names are derived **inside** the library (`rg-dev-payments-api`, `vnet-dev-app-network`). Workload code must not invent ARM names.

### CAF tagging contract

| Source | Keys |
| --- | --- |
| Workload **must** supply | `app`, `costcenter`, `businessunit`, `criticality` |
| Library **always** writes | `env`, `region`, `opsteam=cloud operations` |
| Allowed `env` | `prod`, `staging`, `dev` |
| Allowed `criticality` | `mission-critical`, `medium`, `low` |

Use `CafTags.ToInputMap()`. Do not pass ad-hoc dictionaries with keys that are not in `CafTagCatalog`.

### Verify a restore actually hit GitHub Packages

```bash
rm -rf ~/.nuget/packages/dotcom.cloud.infrastructure.azure/2.2.1 \
       ~/.nuget/packages/dotcom.cloud.infrastructure.common/2.2.1

dotnet restore --no-cache --verbosity detailed | grep -i nuget.pkg.github.com
```

Success looks like `OK` on `dotcom.cloud.infrastructure.azure` / `common` and `Installed ... from https://nuget.pkg.github.com/tech-restack/index.json`. `NotFound` for `Pulumi` on that feed is expected; those packages come from nuget.org. `Forbidden` means the PAT is missing `read:packages` or SSO is not authorized.

---

## Local Development & Build Instructions

Clone and restore:

```bash
git clone git@github.com:tech-restack/dotcom-pulumi-infrastructure.git
cd dotcom-pulumi-infrastructure
dotnet restore Dotcom.Cloud.slnx
```

Build:

```bash
dotnet build Dotcom.Cloud.slnx --configuration Release
```

Test:

```bash
dotnet test Dotcom.Cloud.slnx --configuration Release
```

Pack locally (output: `nupkgs/`, gitignored):

```bash
dotnet pack src/Dotcom.Cloud.Infrastructure.Common/Dotcom.Cloud.Infrastructure.Common.csproj \
  --configuration Release -o nupkgs
dotnet pack src/Dotcom.Cloud.Infrastructure.Azure/Dotcom.Cloud.Infrastructure.Azure.csproj \
  --configuration Release -o nupkgs
```

Do **not** pack AWS or GCP until those projects are implemented and `IsPackable` is set to `true`.

A workload can temporarily consume `nupkgs/` via a local feed. That is for library development only. Production stacks restore from GitHub Packages.

### Versioning

The single version for packable projects is [`Directory.Build.props`](Directory.Build.props) (`Version`). NuGet caches by version. **Always bump** before publishing a behavioural change; never reuse `2.2.1` after it has been pushed.

CI (`.github/workflows/publish-nuget.yml`) runs on every push to `main`: restore → build → test → pack Common + Azure → `dotnet nuget push` to `https://nuget.pkg.github.com/tech-restack/index.json` with `--skip-duplicate`.

---

## Testing

Tests live in `tests/Dotcom.Cloud.Tests` (xUnit, `Microsoft.NET.Test.Sdk`, coverlet collector).

This is **unit testing of infrastructure policy**, not live `pulumi up` against Azure:

| Suite | What it locks |
| --- | --- |
| `NamingConventionsTests` | `{type}-{env}-{name}` lowercase; ACR names alphanumeric, no hyphens |
| `GlobalTagsTests` | Org `env` / `region` / `opsteam` win; required CAF keys; unknown and legacy keys throw |

Preview-time failures in `GlobalTags.Build` are intentional. They stop Cost Management drift before a stack writes tags Azure cannot report on consistently.

When adding an `Org*` component, add tests for any new naming, tag, or argument validation in Common. Do not require a sandbox subscription for the default test run.

---

## Contributing Guidelines

### Branching

- `main` is the publish branch. A merge to `main` ships NuGet packages.
- Feature work: `feature/<ticket-or-short-name>`
- Fixes: `fix/<ticket-or-short-name>`
- Do not force-push `main`.

### Pull requests

1. Open a PR against `main`. One concern per PR (naming, a single `Org*` component, CI, docs).
2. `dotnet build` and `dotnet test` must pass locally.
3. At least one Cloud Operations review is required before merge.
4. Call out **replace** behaviour: changing Pulumi type tokens, Azure resource names, or parent/child graph shape can replace live resources. State that explicitly in the PR.
5. Do not add Datadog (or other product-as-code) resources to this library. Observability product resources belong in workload / CloudOps product repos.

### Version bump (required for packable changes)

If the PR changes Common or Azure runtime behaviour, bump `Version` in `Directory.Build.props` in the **same** PR. Document in the PR body:

- SemVer intent: **patch** (fix), **minor** (new component or additive argument), **major** (breaking caller contract)
- Whether existing stacks need a planned replace

If `Version` is unchanged, CI `--skip-duplicate` will silently skip the push and consumers will keep the old bits.

### Caller contract

- Prefer new optional arguments over breaking required ones.
- Keep `AssignAcrPull` and similar IAM flags explicit; Contributor cannot create role assignments in all subscriptions.
- App Service stays unimplemented until a named workload needs it.

### Security

- No secrets in source, examples, or `nuget.config`.
- Do not enable ACR admin user in components.
- Private DNS VNet links must remain `Location = global`. Do not send empty `DhcpOptions`.

---

## Security & Cost Governance

**Not implemented in CI yet.** The Security Scan and Infracost badges are the target control set, not live gates. Today the only automated pipeline on `main` is restore → test → pack → GitHub Packages (`publish-nuget.yml`).

When these land, pull requests will run:

- **IaC security** (Checkov / Trivy) for misconfiguration: public ingress, overly open NSGs, privileged identities, and secret leakage. Critical findings will block the PR.
- **Infracost** for the monthly spend delta. Unexplained cost spikes will block the PR until the author justifies the change.

Until then, paved-road posture is encoded in the `Org*` defaults and CAF tests only. Do not assume a PR was security- or cost-reviewed because it merged. Workload teams still inherit component defaults from the NuGet packages; they do not get a bypass by copying ARM into `Program.cs`.

---

## Upgrading & Breaking Changes

Pin `Dotcom.Cloud.Infrastructure.Azure` (and, if referenced directly, `Dotcom.Cloud.Infrastructure.Common`) in the workload `.csproj`. Upgrade by changing the version and restoring:

```xml
<PackageReference Include="Dotcom.Cloud.Infrastructure.Azure" Version="2.2.1" />
```

```bash
dotnet restore
pulumi preview
```

- **Patch / minor** (for example `2.2.1` → `2.3.0`): additive. Preview should show updates, not unexpected replaces. Read the PR notes on GitHub Packages for that version.
- **Major** (for example `2.x` → `3.x`): breaking caller contract. Read [`CHANGELOG.md`](CHANGELOG.md) **before** `pulumi up`. Major releases document argument removals, tag catalog changes, and any resources Pulumi will **replace**. Replaces of resource groups, VNets, or Container Apps environments are planned change windows, not drive-by upgrades.

Do not float versions (`Version="2.*"`) on org components. NuGet will pull a new package without a reviewed preview.

---

## Troubleshooting

| Symptom | Cause | Fix |
| --- | --- | --- |
| `403 Forbidden` or `NU1101` / `NU1301` on `dotnet restore` | GitHub Packages rejected the download | Create a classic PAT with `read:packages`, run `dotnet nuget update source github` with that token, then **Enable SSO → Authorize** for `tech-restack`. Rotate if the PAT expired. |
| Preview failed: missing required CAF tag (for example `CAF tag 'app' is required`) | `ExtraTags` omitted a catalog key | Pass all mandatory fields through `CafTags.ToInputMap()`: `App`, `CostCenter`, `BusinessUnit`, `Criticality`. Do not send legacy keys (`application`, `environment`, `owner`). |
| `pulumi preview` reports a missing plugin | Provider plugin not installed for this SDK | Run `pulumi plugin install`, then preview again from the workload directory. |

`NotFound` on `pulumi` / `pulumi.azurenative` against `nuget.pkg.github.com` is expected. Those packages come from nuget.org; only `Dotcom.Cloud.Infrastructure.*` is served from GitHub Packages.

---

## Support

- Source: [tech-restack/dotcom-pulumi-infrastructure](https://github.com/tech-restack/dotcom-pulumi-infrastructure)
- Packages: [GitHub Packages for this repository](https://github.com/orgs/tech-restack/packages?repo_name=dotcom-pulumi-infrastructure)
- Feed: `https://nuget.pkg.github.com/tech-restack/index.json`

Questions about paved-road defaults, CAF tags, or a new `Org*` component: Cloud Operations (`opsteam=cloud operations`).
