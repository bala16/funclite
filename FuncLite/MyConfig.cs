using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite
{
    public class MyConfig
    {
        public string Subscription { get; set; }
        public string ResourceGroup { get; set; }
        public string Region { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public int FreeAppQueueSize { get; set; }
        public int LinuxFreeAppQueueSize { get; set; }
        public string DataFolder { get; set; }
        public string ServerFarmId { get; set; }
        public string DockerServerURL { get; set; }
        public string DockerImageName { get; set; }
        public string DockerRegistryUserName { get; set; }
        public string DockerRegistryPassword { get; set; }
    }
}
