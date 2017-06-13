using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite
{
    public class FunctionVersion
    {
        readonly AppManager _appManager;
        readonly string _packagePath;
        App _app;

        public FunctionVersion(AppManager appManager, string packagePath)
        {
            _appManager = appManager;
            _packagePath = packagePath;
        }

        public async Task Run()
        {
            if (_app == null)
            {
                _app = _appManager.GetApp();
            }

            await _app.SendRequest(new { });
        }
    }
}
