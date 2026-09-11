using Pulumi.AzureNative.Network;
using Inputs = Pulumi.AzureNative.Network.Inputs;

namespace Dotcom.Cloud.Infrastructure.Azure.Core.Vnet
{
    internal static class OrgNetworkBaselines
    {
        public static Inputs.SecurityRuleArgs[] PublicSubnetInbound() =>
        [
            Allow("AllowVnetInbound", 100, "Inbound", "*"),
            Allow("AllowAzureLoadBalancerInbound", 110, "Inbound", "*", source: "AzureLoadBalancer"),
        ];

        public static Inputs.SecurityRuleArgs[] PublicSubnetOutbound() =>
        [
            Allow("AllowVnetOutbound", 100, "Outbound", "*"),
            Allow("AllowHttpsOutbound", 110, "Outbound", "443", destination: "Internet"),
        ];

        public static Inputs.SecurityRuleArgs[] PrivateSubnetInbound() =>
        [
            Allow("AllowAzureLoadBalancerInbound", 100, "Inbound", "*", source: "AzureLoadBalancer"),
            Allow("AllowVnetInbound", 110, "Inbound", "*"),
        ];

        public static Inputs.SecurityRuleArgs[] PrivateSubnetOutbound() =>
        [
            Allow("AllowVnetOutbound", 100, "Outbound", "*"),
            Allow("AllowAzureDnsUdp", 110, "Outbound", "53", protocol: "Udp", destination: "168.63.129.16"),
            Allow("AllowAzureDnsTcp", 111, "Outbound", "53", protocol: "Tcp", destination: "168.63.129.16"),
            Allow("AllowInternetOutbound", 120, "Outbound", "*", destination: "Internet"),
            Allow("AllowNtpOutbound", 130, "Outbound", "123", protocol: "Udp", destination: "Internet"),
            Allow("AllowMicrosoftContainerRegistry", 140, "Outbound", "443", destination: "MicrosoftContainerRegistry"),
            Allow("AllowAzureContainerRegistry", 141, "Outbound", "443", destination: "AzureContainerRegistry"),
        ];

        public static Inputs.SecurityRuleArgs[] PrivateEndpointSubnetInbound() =>
        [
            Allow("AllowVnetInbound", 100, "Inbound", "*"),
        ];

        public static Inputs.SecurityRuleArgs[] PrivateEndpointSubnetOutbound() =>
        [
            Allow("AllowVnetOutbound", 100, "Outbound", "*"),
            Allow("AllowHttpsOutbound", 110, "Outbound", "443", destination: "Internet"),
        ];

        private static Inputs.SecurityRuleArgs Allow(
            string name,
            int priority,
            string direction,
            string destinationPort,
            string protocol = "*",
            string source = "VirtualNetwork",
            string destination = "*")
        {
            return new Inputs.SecurityRuleArgs
            {
                Name = name,
                Priority = priority,
                Direction = direction,
                Access = SecurityRuleAccess.Allow,
                Protocol = protocol,
                SourceAddressPrefix = source,
                SourcePortRange = "*",
                DestinationAddressPrefix = destination,
                DestinationPortRange = destinationPort
            };
        }
    }
}
