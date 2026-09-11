using System;
using System.Collections.Generic;
using System.Linq;
using Pulumi;

namespace Dotcom.Cloud.Infrastructure.Common
{
    public static class GlobalTags
    {
        public const string OpsTeamValue = "cloud operations";

        /// <summary>
        /// Org-owned CAF tags. Workload ExtraTags cannot override env, region, or opsteam.
        /// </summary>
        public static Dictionary<string, string> GetOrgTags(string environment, string location)
        {
            return new Dictionary<string, string>
            {
                { CafTagCatalog.Env, NormalizeEnv(environment) },
                { CafTagCatalog.Region, location.Trim().ToLowerInvariant() },
                { CafTagCatalog.OpsTeam, OpsTeamValue }
            };
        }

        /// <summary>
        /// Merges CAF workload tags with org tags. Unknown keys fail at preview so
        /// teams cannot invent one-off names that break Cost Management reports.
        /// </summary>
        public static Output<Dictionary<string, string>> Resolve(
            Input<string> environment,
            Input<string> location,
            InputMap<string>? extra)
        {
            var extraOutput = (extra ?? new InputMap<string>()).ToOutput();
            return Output.Tuple(environment.ToOutput(), location.ToOutput(), extraOutput)
                .Apply(t => Build(t.Item1, t.Item2, t.Item3));
        }

        public static Dictionary<string, string> Build(
            string environment,
            string location,
            IDictionary<string, string>? extra)
        {
            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (extra != null)
            {
                foreach (var kv in extra)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value))
                    {
                        continue;
                    }

                    var key = kv.Key.Trim().ToLowerInvariant();
                    var value = kv.Value.Trim();

                    if (CafTagCatalog.LegacyKeyRenames.TryGetValue(key, out var cafKey))
                    {
                        throw new ArgumentException(
                            $"Tag '{kv.Key}' is not a Cloud Adoption Framework key. Use '{cafKey}' instead.");
                    }

                    if (!CafTagCatalog.All.Contains(key))
                    {
                        throw new ArgumentException(
                            $"Tag '{kv.Key}' is not in the CAF catalog. Allowed keys: {string.Join(", ", CafTagCatalog.All.Order())}.");
                    }

                    if (CafTagCatalog.AllowedValues.ContainsKey(key))
                    {
                        value = value.ToLowerInvariant();
                    }

                    merged[key] = value;
                }
            }

            foreach (var kv in GetOrgTags(environment, location))
            {
                merged[kv.Key] = kv.Value;
            }

            foreach (var required in CafTagCatalog.RequiredFromWorkload)
            {
                if (!merged.ContainsKey(required))
                {
                    throw new ArgumentException(
                        $"CAF tag '{required}' is required. Set it on CafTags (or ExtraTags) before creating org components.");
                }
            }

            foreach (var kv in merged.ToArray())
            {
                if (CafTagCatalog.AllowedValues.TryGetValue(kv.Key, out var allowed)
                    && !allowed.Contains(kv.Value))
                {
                    throw new ArgumentException(
                        $"CAF tag '{kv.Key}' value '{kv.Value}' is not allowed. Use one of: {string.Join(", ", allowed.Order())}.");
                }
            }

            return new Dictionary<string, string>(merged, StringComparer.Ordinal);
        }

        private static string NormalizeEnv(string environment)
        {
            var env = environment.Trim().ToLowerInvariant();
            return env switch
            {
                "production" => "prod",
                "stage" => "staging",
                "development" => "dev",
                _ => env
            };
        }
    }
}
