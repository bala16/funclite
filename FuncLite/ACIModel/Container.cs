using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite.ACIModel
{
    public class Container
    {
        public string Name { get; }
        public string Image { get; }
        public List<ContainerPort> Ports { get; }

        public Container(string name, string image)
        {
            Name = name;
            Image = image;
            Ports = new List<ContainerPort>();
        }

        public void AddPort(ContainerPort containerPort)
        {
            Ports.Add(containerPort);
        }
    }
}
