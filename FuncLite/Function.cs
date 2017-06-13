using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite
{
    public class Function
    {
        readonly AppManager _appManager;
        Dictionary<int, FunctionVersion> _versions = new Dictionary<int, FunctionVersion>();
        readonly string _folder;

        public Function(AppManager appManager, string folder)
        {
            _appManager = appManager;
            _folder = folder;
            Directory.CreateDirectory(_folder);
        }

        public async Task Create(Stream zipContent)
        {
            int newVersion = GetNewVersion();

            string versionFolder = Path.Combine(_folder, newVersion.ToString());
            Directory.CreateDirectory(versionFolder);
            string packagePath = Path.Combine(versionFolder, "package.zip");
            using (Stream zip = File.Create(packagePath))
            {
                await zipContent.CopyToAsync(zip);
            }

            _versions[newVersion] = new FunctionVersion(_appManager, packagePath);
        }

        public async Task Run()
        {
            int latestVersion = GetLatestVersion();
            if (latestVersion <= 0)
            {
                throw new Exception($"Function doesn'thave any versions");
            }

            var funcVersion = _versions[latestVersion];
            await funcVersion.Run();
        }

        int GetLatestVersion()
        {
            return _versions.Keys.OrderByDescending(v => v).FirstOrDefault();
        }

        int GetNewVersion()
        {
            return GetLatestVersion() + 1;
        }
    }
}
