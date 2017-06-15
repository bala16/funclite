using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuncLite
{
    public class FunctionManager
    {
        readonly AppManager _appManager;
        readonly string _functionsFolder;
        readonly Dictionary<Language, Dictionary<string, Function>> _functions;

        public FunctionManager(IOptions<MyConfig> config, AppManager appManager, ILogger<FunctionManager> logger)
        {
            _appManager = appManager;
            _functionsFolder = Path.Combine(config.Value.DataFolder, "Functions");
            Directory.CreateDirectory(_functionsFolder);

            _functions = new Dictionary<Language, Dictionary<string, Function>>();
            foreach (var language in Enum.GetValues(typeof(Language)).Cast<Language>())
            {
                _functions.Add(language, new Dictionary<string, Function>(StringComparer.OrdinalIgnoreCase));
            }

            LoadExistingFunctions();
        }

        public IEnumerable<string> Functions
        {
            get
            {
                var functions = new List<string>();
                foreach (var value in _functions.Values)
                {
                    functions.AddRange(value.Keys);
                }
                return functions;
            }
        }

        public async Task<Function> Create(Language language, string name, Stream zipContent)
        {
            Function function = EnsureFunction(language, name);
            await function.CreateNewVersion(zipContent);
            return function;
        }

        public async Task<dynamic> Run(string name, int? version, JObject requestBody)
        {
            var function = GetFunction(Language.Ruby, name, false) ?? GetFunction(Language.Node, name, true);
            return await function.Run(requestBody, version);
        }

        void LoadExistingFunctions()
        {
            foreach (string languageFolderPath in Directory.EnumerateDirectories(_functionsFolder))
            {
                string languageName = Path.GetFileName(languageFolderPath);
                if (Enum.TryParse(languageName, out Language language))
                {
                    foreach (var functionFolderPath in Directory.EnumerateDirectories(languageFolderPath))
                    {
                        string functionName = Path.GetFileName(functionFolderPath);
                        _functions[language][functionName] = new Function(_appManager, language, functionFolderPath);
                    }
                }
            }
        }

        public Function GetFunction(Language language, string name, bool throwIfNotFound = false)
        {
            var functions = _functions[language];

            if (!functions.TryGetValue(name, out Function function))
            {
                if (throwIfNotFound)
                {
                    throw new FileNotFoundException($"Function {name} does not exist");
                }
                return null;
            }

            return function;
        }


        Function EnsureFunction(Language language, string name)
        {
            var functions = _functions[language];

            if (!functions.TryGetValue(name, out Function function))
            {
                function = functions[name] = new Function(_appManager, language, Path.Combine(new[] { _functionsFolder, language.ToString(), name }));
            }

            return function;
        }

        public async Task DeleteFunction(string name)
        {
            Function function = GetFunction(Language.Node, name, throwIfNotFound: false) ?? GetFunction(Language.Ruby, name, throwIfNotFound: true);

            await function.Delete();
            _functions[function.Language].Remove(name);
        }
    }
}
