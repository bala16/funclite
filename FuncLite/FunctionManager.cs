using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite
{
    public class FunctionManager
    {
        readonly AppManager _appManager;
        readonly string _functionsFolder;
        Dictionary<string, Function> _functions = new Dictionary<string, Function>(StringComparer.OrdinalIgnoreCase);

        public FunctionManager(IOptions<MyConfig> config, AppManager appManager)
        {
            _appManager = appManager;
            _functionsFolder = Path.Combine(config.Value.DataFolder, "Functions");
            Directory.CreateDirectory(_functionsFolder);
        }

        public async Task<Function> Create(string name, Stream zipContent)
        {
            Function function = GetFunction(name);
            await function.Create(zipContent);
            return function;
        }

        Function GetFunction(string name)
        {
            if (!_functions.TryGetValue(name, out Function function))
            {
                function = new Function(_appManager, Path.Combine(_functionsFolder, name));
                _functions[name] = function;
            }

            return function;
        }
    }
}
