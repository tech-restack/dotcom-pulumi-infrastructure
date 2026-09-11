using System.Collections.Generic;
using Pulumi;

namespace Dotcom.Cloud.Infrastructure.Common
{
    /// <summary>
    /// Workload tags from the Microsoft Cloud Adoption Framework tagging strategy.
    /// https://learn.microsoft.com/azure/cloud-adoption-framework/ready/azure-best-practices/resource-tagging
    ///
    /// Do not set env, region, or opsteam here. The org library always writes those
    /// from the component Environment, Location, and Cloud Operations ownership.
    /// </summary>
    public sealed class CafTags
    {
        // Functional (required: App)
        public required string App { get; init; }
        public string? Tier { get; init; }
        public string? WebServer { get; init; }
        public string? Repo { get; init; }

        // Classification (required: Criticality)
        public required string Criticality { get; init; }
        public string? Confidentiality { get; init; }
        public string? Sla { get; init; }

        // Accounting (required: CostCenter)
        public required string CostCenter { get; init; }
        public string? Department { get; init; }
        public string? Program { get; init; }
        public string? BusinessCenter { get; init; }
        public string? Budget { get; init; }

        // Purpose
        public string? BusinessProcess { get; init; }
        public string? BusinessImpact { get; init; }
        public string? RevenueImpact { get; init; }

        // Ownership (required: BusinessUnit)
        public required string BusinessUnit { get; init; }

        /// <summary>
        /// Converts to the InputMap Org* components accept as ExtraTags.
        /// </summary>
        public InputMap<string> ToInputMap()
        {
            var map = new InputMap<string>();
            Add(map, CafTagCatalog.App, App);
            Add(map, CafTagCatalog.Tier, Tier);
            Add(map, CafTagCatalog.WebServer, WebServer);
            Add(map, CafTagCatalog.Repo, Repo);
            Add(map, CafTagCatalog.Criticality, Criticality);
            Add(map, CafTagCatalog.Confidentiality, Confidentiality);
            Add(map, CafTagCatalog.Sla, Sla);
            Add(map, CafTagCatalog.CostCenter, CostCenter);
            Add(map, CafTagCatalog.Department, Department);
            Add(map, CafTagCatalog.Program, Program);
            Add(map, CafTagCatalog.BusinessCenter, BusinessCenter);
            Add(map, CafTagCatalog.Budget, Budget);
            Add(map, CafTagCatalog.BusinessProcess, BusinessProcess);
            Add(map, CafTagCatalog.BusinessImpact, BusinessImpact);
            Add(map, CafTagCatalog.RevenueImpact, RevenueImpact);
            Add(map, CafTagCatalog.BusinessUnit, BusinessUnit);
            return map;
        }

        private static void Add(InputMap<string> map, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                map.Add(key, value.Trim());
            }
        }
    }

    /// <summary>
    /// CAF tag names (lowercase). Azure tag names are case-insensitive; values are case-sensitive.
    /// </summary>
    public static class CafTagCatalog
    {
        public const string App = "app";
        public const string Tier = "tier";
        public const string WebServer = "webserver";
        public const string Env = "env";
        public const string Region = "region";
        public const string Repo = "repo";
        public const string Criticality = "criticality";
        public const string Confidentiality = "confidentiality";
        public const string Sla = "sla";
        public const string Department = "department";
        public const string Program = "program";
        public const string BusinessCenter = "businesscenter";
        public const string Budget = "budget";
        public const string CostCenter = "costcenter";
        public const string BusinessProcess = "businessprocess";
        public const string BusinessImpact = "businessimpact";
        public const string RevenueImpact = "revenueimpact";
        public const string BusinessUnit = "businessunit";
        public const string OpsTeam = "opsteam";

        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            App, Tier, WebServer, Env, Region, Repo,
            Criticality, Confidentiality, Sla,
            Department, Program, BusinessCenter, Budget, CostCenter,
            BusinessProcess, BusinessImpact, RevenueImpact,
            BusinessUnit, OpsTeam
        };

        public static readonly IReadOnlyList<string> RequiredFromWorkload =
        [
            App, CostCenter, BusinessUnit, Criticality
        ];

        public static readonly IReadOnlyDictionary<string, string> LegacyKeyRenames =
            new Dictionary<string, string>
            {
                ["environment"] = Env,
                ["application"] = App,
                ["owner"] = BusinessUnit,
                ["managed-by"] = OpsTeam,
                ["repository"] = Repo
            };

        public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedValues =
            new Dictionary<string, IReadOnlySet<string>>
            {
                [Env] = new HashSet<string> { "prod", "staging", "dev" },
                [Criticality] = new HashSet<string> { "mission-critical", "medium", "low" },
                [Confidentiality] = new HashSet<string> { "public", "internal", "confidential", "private" },
                [BusinessImpact] = new HashSet<string> { "low", "moderate", "high" },
                [RevenueImpact] = new HashSet<string> { "low", "moderate", "high" },
                [Tier] = new HashSet<string> { "web", "api", "data", "cache", "network", "platform", "infra" }
            };
    }
}
