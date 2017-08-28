using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json.Linq;

namespace FuncLite.ACIModel
{
    public class ContainerGroup
    {
        private readonly List<Container> _containers;
        public List<uint> PublicPorts { get; }
        private readonly string _location;
        public string IpAddress { get; set; }

        public string Name { get; }

        public ContainerGroup(string name, string location, string ipAddress)
        {
            Name = name;
            _location = location;
            IpAddress = ipAddress;
            _containers = new List<Container>();
            PublicPorts = new List<uint>();
        }

        public void AddContainer(Container container)
        {
            _containers.Add(container);
            foreach (var containerPort in container.Ports)
            {
                if (containerPort.IsPublic)
                {
                    PublicPorts.Add(containerPort.Port);
                }
            }
        }

        public ContainerGroup Duplicate()
        {
            var newContainerGroupName = ACIUtils.GetContainerGroupNameForAppName(ACIUtils.GetAppNameFromContainerGroupName(Name));
            var newContainerGroup = new ContainerGroup(newContainerGroupName, _location, "");
            foreach (var container in _containers)
            {
                newContainerGroup.AddContainer(container);
            }
            return newContainerGroup;
        }

        public JObject ToACICreateContainerRequest()
        {
            var portsArray = new JArray();

            foreach (var publicPort in PublicPorts)
            {
                var protocol = new JProperty("protocol", "TCP");
                var port = new JProperty("port", publicPort);
                var portDefinition = new JObject(protocol, port);
                portsArray.Add(portDefinition);
            }

            var containersArray = new JArray();
            foreach (var container in _containers)
            {
                var containerPortArray = new JArray();
                foreach (var containerPort in container.Ports)
                {
                    var containerPortProperty = new JProperty("port", containerPort.Port);
                    var containerPortDefinition = new JObject(containerPortProperty);
                    containerPortArray.Add(containerPortDefinition);
                }

                var requests = new JObject {new JProperty("memoryInGb", 1.5), new JProperty("cpu", 1.0)};
                var resources = new JObject {{"requests", requests}};

                var properties = new JObject
                {
                    new JProperty("image", container.Image),
                    new JProperty("ports", containerPortArray),
                    {"resources", resources}
                };

                var containerItemJObject =
                    new JObject {new JProperty("name", container.Name), {"properties", properties}};

                containersArray.Add(containerItemJObject);
            }


            dynamic ipAddress = new JObject();
            ipAddress.ports = portsArray;
            ipAddress.type = "Public";

            dynamic containerGroupProperties = new JObject();
            containerGroupProperties.osType = "Linux";
            containerGroupProperties.ipAddress = ipAddress;
            containerGroupProperties.containers = containersArray;

            dynamic containerGroup = new JObject();
            containerGroup.location = _location;
            containerGroup.properties = containerGroupProperties;

            return containerGroup;
        }
    }
}
