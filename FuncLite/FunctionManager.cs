using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
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

        public FunctionManager(IOptions<MyConfig> config, AppManager appManager, ILogger<FunctionManager> logger)
        {
            _appManager = appManager;
            _functionsFolder = Path.Combine(config.Value.DataFolder, "Functions");
            Directory.CreateDirectory(_functionsFolder);

            LoadExistingFunctions();
        }

        public IEnumerable<string> Functions => _functions.Keys;

        public async Task<Function> Create(string name, Stream zipContent)
        {
            Function function = GetFunction(name);
            await function.CreateNewVersion(zipContent);
            return function;
        }

        public async Task<dynamic> Run(string name, JObject requestBody)
        {
            if (!_functions.TryGetValue(name, out Function function))
            {
                throw new FileNotFoundException($"Function {name} does not exist");
            }

            return await function.Run(requestBody);
        }

        void LoadExistingFunctions()
        {
            foreach (string functionFolderPath in Directory.EnumerateDirectories(_functionsFolder))
            {
                string functionName = Path.GetFileName(functionFolderPath);
                _functions[functionName] = new Function(_appManager, functionFolderPath);
            }
        }

        Function GetFunction(string name)
        {
            if (!_functions.TryGetValue(name, out Function function))
            {
                _functions[name] = new Function(_appManager, Path.Combine(_functionsFolder, name));
            }

            return function;
        }
    }
}
