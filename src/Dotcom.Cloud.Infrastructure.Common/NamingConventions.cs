using System.Linq;

namespace Dotcom.Cloud.Infrastructure.Common
{
    public static class NamingConventions
    {
        public static string GetResourceName(string componentName, string environment, string resourceType)
        {
            return $"{resourceType}-{environment}-{componentName}".ToLowerInvariant();
        }

        /// <summary>
        /// ACR names must be 5-50 alphanumeric characters with no hyphens.
        /// </summary>
        public static string GetAcrName(string componentName, string environment)
        {
            var alnum = new string($"acr{environment}{componentName}".Where(char.IsLetterOrDigit).ToArray())
                .ToLowerInvariant();

            if (alnum.Length < 5)
            {
                alnum += "reg";
            }

            return alnum.Length <= 50 ? alnum : alnum[..50];
        }
    }
}
