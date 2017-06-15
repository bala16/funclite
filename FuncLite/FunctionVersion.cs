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
        BaseApp _app;

        public FunctionVersion(AppManager appManager, string zipPackagePath)
        {
            _appManager = appManager;
            _zipPackagePath = zipPackagePath;
        }

        public async Task<dynamic> Run(Language language, JObject requestBody)
        {
            if (_app == null)
            {
                _app = _appManager.GetAppFor(language);

                if (language == Language.Ruby)
                {
                    var linuxApp = _app as LinuxApp;
                    await linuxApp.UploadUserCode(_zipPackagePath);
                }
                else
                {
                    var windowsApp = _app as WindowsApp;
                    await windowsApp.UploadUserCode(_zipPackagePath);
                }

            }

            var json = await _app.SendRequest(new { functionBody = requestBody });
            return json.functionBody;
        }

        public async Task Delete()
        {
            if (_app != null)
            {
                await _app.Delete();
                _app = null;
            }
        }
    }
}
