using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite
{
    public class ServiceFabricApp
    {
        public string AppName { get; }
        public string ServiceName { get; }

        public ServiceFabricApp(string appName, string serviceName)
        {
            AppName = appName;
            ServiceName = serviceName;
        }
    }
}
