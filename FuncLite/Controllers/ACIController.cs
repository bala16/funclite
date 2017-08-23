using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FuncLite.Controllers
{
    [Route("api/[controller]")]
    public class ACIController : Controller
    {
        private readonly ACIManager _aciManager;

        public ACIController(ACIManager aciManager)
        {
            _aciManager = aciManager;
        }

        [HttpGet]
        public dynamic GetApps()
        {
            return JsonConvert.SerializeObject(_aciManager.GetApps(), Formatting.Indented);
        }
    }
}
