using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FuncLite.ACIModel
{
    public class ContainerGroupCollection
    {
        private const int MinAppInstanceCount = 2;
        private const int MaxAppInstanceCount = 4;

        private readonly string _appName;
        private readonly List<ContainerGroup> _containerGroups;
        private int _nextContainerIndex;
        public ContainerGroupCollection(string appName)
        {
            _appName = appName;
            _containerGroups = new List<ContainerGroup>();
            _nextContainerIndex = 0;
        }

        public bool CanScaleUp()
        {
            return _containerGroups.Count < MaxAppInstanceCount;
        }

        public bool CanScaleDown()
        {
            return _containerGroups.Count > MinAppInstanceCount;
        }

        public IEnumerable<string> GetIpAddresses()
        {
            return _containerGroups.Select(group => group.IpAddress);
        }

        public void AddContainerGroup(ContainerGroup containerGroup)
        {
            _nextContainerIndex = 0;
            _containerGroups.Add(containerGroup);
        }

        public IEnumerable<string> GetContainerGroupNames()
        {
            return _containerGroups.Select(group => group.Name);
        }

        public ContainerGroup GetNextContainerGroup()
        {
            if (_nextContainerIndex < _containerGroups.Count)
            {
                var nextContainerGroup = _containerGroups[_nextContainerIndex];
                _nextContainerIndex = (_nextContainerIndex + 1) % _containerGroups.Count;
                return nextContainerGroup;
            }
            return null;
        }

        private ContainerGroup GetRandomContainerGroup()
        {
            var random = new Random();
            var next = random.Next(0, _containerGroups.Count);
            return _containerGroups[next];
        }

        public ContainerGroup RemoveNextContainerGroup()
        {
            var nextContainerGroup = GetNextContainerGroup();
            _containerGroups.Remove(nextContainerGroup);
            _nextContainerIndex = 0;
            return nextContainerGroup;
        }
    }
}
