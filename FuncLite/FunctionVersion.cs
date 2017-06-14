using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite
{
    public class FunctionVersion
    {
        readonly AppManager _appManager;
        readonly string _zipPackagePath;
        App _app;

        public FunctionVersion(AppManager appManager, string zipPackagePath)
        {
            _appManager = appManager;
            _zipPackagePath = zipPackagePath;
        }

        public async Task<dynamic> Run(JObject requestBody)
        {
            if (_app == null)
            {
                _app = _appManager.GetApp();
                await _app.UploadUserCode(_zipPackagePath);
            }

            var json = await _app.SendRequest(new { functionBody = requestBody });
            return json.functionBody;
        }
    }
}
