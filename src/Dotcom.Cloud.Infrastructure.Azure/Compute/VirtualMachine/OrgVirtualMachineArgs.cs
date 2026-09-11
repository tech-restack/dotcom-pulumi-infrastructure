using Pulumi;
using Dotcom.Cloud.Infrastructure.Azure.Core.ResourceGroup;

namespace Dotcom.Cloud.Infrastructure.Azure.Compute.VirtualMachine;

public enum OrgVmOsType
{
    Linux,
    Windows
}

public sealed class OrgVmImageReferenceArgs : ResourceArgs
{
    [Input("publisher", required: true)]
    public Input<string> Publisher { get; set; } = null!;

    [Input("offer", required: true)]
    public Input<string> Offer { get; set; } = null!;

    [Input("sku", required: true)]
    public Input<string> Sku { get; set; } = null!;

    [Input("version")]
    public Input<string>? Version { get; set; }
}

public sealed class OrgVmDataDiskArgs : ResourceArgs
{
    [Input("lun", required: true)]
    public Input<int> Lun { get; set; } = null!;

    [Input("sizeGb", required: true)]
    public Input<int> SizeGb { get; set; } = null!;

    [Input("storageAccountType")]
    public Input<string>? StorageAccountType { get; set; }
}

public class OrgVirtualMachineArgs : ResourceArgs
{
    [Input("environment", required: true)]
    public Input<string> Environment { get; set; } = null!;

    [Input("resourceGroup", required: true)]
    public OrgResourceGroup ResourceGroup { get; set; } = null!;

    /// <summary>
    /// Existing subnet ID. Do not use the Container Apps-delegated private subnet from OrgVnet.
    /// Use PublicSubnetId or a dedicated compute subnet.
    /// </summary>
    [Input("subnetId", required: true)]
    public Input<string> SubnetId { get; set; } = null!;

    [Input("osType")]
    public OrgVmOsType OsType { get; set; } = OrgVmOsType.Linux;

    [Input("location")]
    public Input<string>? Location { get; set; }

    /// <summary>
    /// Default Standard_D2s_v5 (supports accelerated networking).
    /// </summary>
    [Input("vmSize")]
    public Input<string>? VmSize { get; set; }

    [Input("imageReference")]
    public OrgVmImageReferenceArgs? ImageReference { get; set; }

    [Input("adminUsername")]
    public Input<string>? AdminUsername { get; set; }

    /// <summary>
    /// Required for Linux. Password authentication is disabled.
    /// </summary>
    [Input("sshPublicKey")]
    public Input<string>? SshPublicKey { get; set; }

    /// <summary>
    /// Required for Windows. Pass a Pulumi secret or Key Vault Output; never a plaintext default.
    /// </summary>
    [Input("adminPasswordSecret")]
    public Input<string>? AdminPasswordSecret { get; set; }

    [Input("osDiskSizeGb")]
    public Input<int>? OsDiskSizeGb { get; set; }

    [Input("dataDisks")]
    public InputList<OrgVmDataDiskArgs> DataDisks { get; set; } = new();

    /// <summary>
    /// Disk Encryption Set resource ID for CMK. Omit for platform-managed keys (default).
    /// </summary>
    [Input("diskEncryptionSetId")]
    public Input<string>? DiskEncryptionSetId { get; set; }

    public bool EnableAcceleratedNetworking { get; set; } = true;

    public bool EnableEncryptionAtHost { get; set; } = true;

    public bool EnableEntraSshLogin { get; set; } = true;

    public bool EnableAzureMonitorAgent { get; set; } = true;

    [Input("extraTags")]
    public InputMap<string> ExtraTags { get; set; } = new();
}
