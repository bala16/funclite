using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite.ACIModel
{
    public static class ACIUtils
    {
        public static string GetAppNameFromContainerGroupName(string containerGroupName)
        {
            var indexOf = containerGroupName.IndexOf("-", StringComparison.OrdinalIgnoreCase);
            return containerGroupName.Substring(0, indexOf).ToLowerInvariant();
        }

        public static string GetContainerGroupNameForAppName(string appName)
        {
            return $"{appName}-{Guid.NewGuid()}";
        }
    }
}
