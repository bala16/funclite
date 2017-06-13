using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite
{
    public class Function
    {
        Dictionary<int, FunctionVersion> _versions = new Dictionary<int, FunctionVersion>();
        readonly string _folder;

        public Function(string folder)
        {
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

            _versions[newVersion] = new FunctionVersion(packagePath);
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
