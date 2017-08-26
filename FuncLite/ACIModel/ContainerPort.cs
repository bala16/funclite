using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite.ACIModel
{
    public class ContainerPort
    {
        public uint Port { get; }
        public bool IsPublic { get; }

        public ContainerPort(uint port, bool isPublic)
        {
            Port = port;
            IsPublic = isPublic;
        }
    }
}
