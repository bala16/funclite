using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FuncLite
{
    public class ACIManager
    {
        private readonly RawACIManager _rawAciManager;
        private readonly Dictionary<string, ACIApp> _apps;

        public ACIManager(RawACIManager rawAciManager)
        {
            _rawAciManager = rawAciManager;
            _apps = new Dictionary<string, ACIApp>();
        }

        public Dictionary<string, ACIApp>.KeyCollection GetApps()
        {
            return _apps.Keys;
        }

        public async Task<List<ACIApp>> GetAppsFromARM()
        {
            var aciApps = new List<ACIApp>();
            var containerGroups = await _rawAciManager.GetContainerGroups();

            foreach (var containerGroup in containerGroups.value)
            {
                var containerProperties = containerGroup.properties.containers[0].properties;
                string image = containerProperties.image;
                var aciApp = new ACIApp(image);
                aciApps.Add(aciApp);
            }
            return aciApps;
        }
    }
}
