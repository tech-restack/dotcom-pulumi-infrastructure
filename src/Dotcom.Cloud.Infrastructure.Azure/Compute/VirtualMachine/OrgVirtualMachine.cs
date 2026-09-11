using System;
using System.Collections.Generic;
using System.Linq;
using Pulumi;
using AzureNative = Pulumi.AzureNative;
using Dotcom.Cloud.Infrastructure.Common;

namespace Dotcom.Cloud.Infrastructure.Azure.Compute.VirtualMachine;

public class OrgVirtualMachine : ComponentResource
{
    [Output("id")]
    public Output<string> Id { get; private set; } = null!;

    [Output("name")]
    public Output<string> Name { get; private set; } = null!;

    [Output("privateIpAddress")]
    public Output<string> PrivateIpAddress { get; private set; } = null!;

    [Output("principalId")]
    public Output<string> PrincipalId { get; private set; } = null!;

    [Output("networkInterfaceId")]
    public Output<string> NetworkInterfaceId { get; private set; } = null!;

    public OrgVirtualMachine(string name, OrgVirtualMachineArgs args, ComponentResourceOptions? options = null)
        : base("dotcom:compute:OrgVirtualMachine", name, args, options)
    {
        ArgumentNullException.ThrowIfNull(args.SubnetId);

        if (args.OsType == OrgVmOsType.Linux && args.SshPublicKey is null)
        {
            throw new ArgumentException("Linux VMs require SshPublicKey. Password authentication is disabled.");
        }

        if (args.OsType == OrgVmOsType.Windows && args.AdminPasswordSecret is null)
        {
            throw new ArgumentException("Windows VMs require AdminPasswordSecret (Pulumi secret or Key Vault Output).");
        }

        var location = args.Location ?? args.ResourceGroup.Location;
        var tags = GlobalTags.Resolve(args.Environment, location, args.ExtraTags);
        var vmSize = args.VmSize ?? "Standard_D2s_v5";
        var adminUser = args.AdminUsername ?? "azureuser";
        var osDiskGb = args.OsDiskSizeGb ?? (args.OsType == OrgVmOsType.Windows ? 127 : 64);
        var accelerated = args.EnableAcceleratedNetworking;
        var encryptionAtHost = args.EnableEncryptionAtHost;
        var entraSsh = args.EnableEntraSshLogin;
        var ama = args.EnableAzureMonitorAgent;
        var opts = new CustomResourceOptions { Parent = this };

        var vmName = args.Environment.Apply(env => NamingConventions.GetResourceName(name, env, "vm"));
        var nicName = args.Environment.Apply(env => NamingConventions.GetResourceName(name, env, "nic"));
        var osDiskName = args.Environment.Apply(env => NamingConventions.GetResourceName(name, env, "disk"));

        var nic = new AzureNative.Network.NetworkInterface($"{name}-nic", new()
        {
            ResourceGroupName = args.ResourceGroup.Name,
            NetworkInterfaceName = nicName,
            Location = location,
            EnableAcceleratedNetworking = accelerated,
            EnableIPForwarding = false,
            IpConfigurations =
            {
                new AzureNative.Network.Inputs.NetworkInterfaceIPConfigurationArgs
                {
                    Name = "ipconfig1",
                    Primary = true,
                    PrivateIPAllocationMethod = AzureNative.Network.IPAllocationMethod.Dynamic,
                    Subnet = new AzureNative.Network.Inputs.SubnetArgs
                    {
                        Id = args.SubnetId
                    }
                }
            },
            Tags = tags
        }, opts);

        var image = args.ImageReference ?? DefaultImage(args.OsType);

        var osDisk = new AzureNative.Compute.Inputs.OSDiskArgs
        {
            Name = osDiskName,
            CreateOption = AzureNative.Compute.DiskCreateOptionTypes.FromImage,
            Caching = AzureNative.Compute.CachingTypes.ReadWrite,
            DiskSizeGB = osDiskGb,
            DeleteOption = AzureNative.Compute.DiskDeleteOptionTypes.Delete,
            ManagedDisk = ManagedDisk(args.DiskEncryptionSetId)
        };

        var dataDisks = args.DataDisks.ToOutput().Apply(list =>
            list.Select((disk, index) =>
            {
                var lun = disk.Lun;
                return new AzureNative.Compute.Inputs.DataDiskArgs
                {
                    Lun = lun,
                    DiskSizeGB = disk.SizeGb,
                    CreateOption = AzureNative.Compute.DiskCreateOptionTypes.Empty,
                    Caching = AzureNative.Compute.CachingTypes.ReadOnly,
                    DeleteOption = AzureNative.Compute.DiskDeleteOptionTypes.Delete,
                    Name = args.Environment.Apply(env =>
                        NamingConventions.GetResourceName($"{name}-{index}", env, "disk")),
                    ManagedDisk = ManagedDisk(args.DiskEncryptionSetId, disk.StorageAccountType)
                };
            }).ToArray());

        var osProfile = BuildOsProfile(args, adminUser, vmName, name);

        var vm = new AzureNative.Compute.VirtualMachine(name, new()
        {
            ResourceGroupName = args.ResourceGroup.Name,
            VmName = vmName,
            Location = location,
            HardwareProfile = new AzureNative.Compute.Inputs.HardwareProfileArgs
            {
                VmSize = vmSize
            },
            NetworkProfile = new AzureNative.Compute.Inputs.NetworkProfileArgs
            {
                NetworkInterfaces =
                {
                    new AzureNative.Compute.Inputs.NetworkInterfaceReferenceArgs
                    {
                        Id = nic.Id,
                        Primary = true,
                        DeleteOption = AzureNative.Compute.DeleteOptions.Delete
                    }
                }
            },
            OsProfile = osProfile,
            StorageProfile = new AzureNative.Compute.Inputs.StorageProfileArgs
            {
                ImageReference = new AzureNative.Compute.Inputs.ImageReferenceArgs
                {
                    Publisher = image.Publisher,
                    Offer = image.Offer,
                    Sku = image.Sku,
                    Version = image.Version ?? "latest"
                },
                OsDisk = osDisk,
                DataDisks = dataDisks
            },
            Identity = new AzureNative.Compute.Inputs.VirtualMachineIdentityArgs
            {
                Type = AzureNative.Compute.ResourceIdentityType.SystemAssigned
            },
            DiagnosticsProfile = new AzureNative.Compute.Inputs.DiagnosticsProfileArgs
            {
                BootDiagnostics = new AzureNative.Compute.Inputs.BootDiagnosticsArgs
                {
                    Enabled = true
                }
            },
            SecurityProfile = new AzureNative.Compute.Inputs.SecurityProfileArgs
            {
                EncryptionAtHost = encryptionAtHost
            },
            Tags = tags
        }, opts);

        if (args.OsType == OrgVmOsType.Linux && entraSsh)
        {
            _ = new AzureNative.Compute.VirtualMachineExtension($"{name}-aadssh", new()
            {
                ResourceGroupName = args.ResourceGroup.Name,
                VmName = vm.Name,
                Location = location,
                Type = "AADSSHLoginForLinux",
                Publisher = "Microsoft.Azure.ActiveDirectory",
                TypeHandlerVersion = "1.0",
                AutoUpgradeMinorVersion = true,
                EnableAutomaticUpgrade = true,
                Tags = tags
            }, opts);
        }

        if (ama)
        {
            var (publisher, extensionType) = args.OsType == OrgVmOsType.Windows
                ? ("Microsoft.Azure.Monitor", "AzureMonitorWindowsAgent")
                : ("Microsoft.Azure.Monitor", "AzureMonitorLinuxAgent");

            _ = new AzureNative.Compute.VirtualMachineExtension($"{name}-ama", new()
            {
                ResourceGroupName = args.ResourceGroup.Name,
                VmName = vm.Name,
                Location = location,
                Type = extensionType,
                Publisher = publisher,
                TypeHandlerVersion = "1.0",
                AutoUpgradeMinorVersion = true,
                EnableAutomaticUpgrade = true,
                Tags = tags
            }, opts);
        }

        this.Id = vm.Id;
        this.Name = vm.Name;
        this.NetworkInterfaceId = nic.Id;
        this.PrivateIpAddress = nic.IpConfigurations.Apply(cfgs =>
            cfgs.Length == 0 ? string.Empty : cfgs[0].PrivateIPAddress ?? string.Empty);
        this.PrincipalId = vm.Identity.Apply(i => i?.PrincipalId ?? string.Empty);

        this.RegisterOutputs(new Dictionary<string, object?>
        {
            { "id", this.Id },
            { "name", this.Name },
            { "privateIpAddress", this.PrivateIpAddress },
            { "principalId", this.PrincipalId },
            { "networkInterfaceId", this.NetworkInterfaceId }
        });
    }

    private static AzureNative.Compute.Inputs.OSProfileArgs BuildOsProfile(
        OrgVirtualMachineArgs args,
        Input<string> adminUser,
        Output<string> vmName,
        string logicalName)
    {
        var profile = new AzureNative.Compute.Inputs.OSProfileArgs
        {
            ComputerName = args.OsType == OrgVmOsType.Windows
                ? args.Environment.Apply(env => NamingConventions.GetWindowsComputerName(logicalName, env))
                : vmName,
            AdminUsername = adminUser
        };

        if (args.OsType == OrgVmOsType.Linux)
        {
            profile.LinuxConfiguration = new AzureNative.Compute.Inputs.LinuxConfigurationArgs
            {
                DisablePasswordAuthentication = true,
                ProvisionVMAgent = true,
                Ssh = new AzureNative.Compute.Inputs.SshConfigurationArgs
                {
                    PublicKeys =
                    {
                        new AzureNative.Compute.Inputs.SshPublicKeyArgs
                        {
                            Path = adminUser.Apply(user => $"/home/{user}/.ssh/authorized_keys"),
                            KeyData = args.SshPublicKey!
                        }
                    }
                },
                PatchSettings = new AzureNative.Compute.Inputs.LinuxPatchSettingsArgs
                {
                    PatchMode = AzureNative.Compute.LinuxVMGuestPatchMode.AutomaticByPlatform,
                    AssessmentMode = AzureNative.Compute.LinuxPatchAssessmentMode.AutomaticByPlatform
                }
            };
        }
        else
        {
            profile.AdminPassword = args.AdminPasswordSecret;
            profile.WindowsConfiguration = new AzureNative.Compute.Inputs.WindowsConfigurationArgs
            {
                ProvisionVMAgent = true,
                EnableAutomaticUpdates = true,
                PatchSettings = new AzureNative.Compute.Inputs.PatchSettingsArgs
                {
                    PatchMode = AzureNative.Compute.WindowsVMGuestPatchMode.AutomaticByPlatform,
                    AssessmentMode = AzureNative.Compute.WindowsPatchAssessmentMode.AutomaticByPlatform
                }
            };
        }

        return profile;
    }

    private static AzureNative.Compute.Inputs.ManagedDiskParametersArgs ManagedDisk(
        Input<string>? diskEncryptionSetId,
        Input<string>? storageAccountType = null)
    {
        var disk = new AzureNative.Compute.Inputs.ManagedDiskParametersArgs();
        if (storageAccountType is not null)
        {
            disk.StorageAccountType = storageAccountType;
        }
        else
        {
            disk.StorageAccountType = AzureNative.Compute.StorageAccountTypes.Premium_LRS;
        }

        if (diskEncryptionSetId is not null)
        {
            disk.DiskEncryptionSet = new AzureNative.Compute.Inputs.DiskEncryptionSetParametersArgs
            {
                Id = diskEncryptionSetId
            };
        }

        return disk;
    }

    private static OrgVmImageReferenceArgs DefaultImage(OrgVmOsType osType) =>
        osType == OrgVmOsType.Windows
            ? new OrgVmImageReferenceArgs
            {
                Publisher = "MicrosoftWindowsServer",
                Offer = "WindowsServer",
                Sku = "2022-datacenter-g2",
                Version = "latest"
            }
            : new OrgVmImageReferenceArgs
            {
                Publisher = "Canonical",
                Offer = "0001-com-ubuntu-server-jammy",
                Sku = "22_04-lts-gen2",
                Version = "latest"
            };
}
