using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite
{
    public class Function
    {
        const string ZipFileName = "package.zip";
        readonly AppManager _appManager;
        Dictionary<int, FunctionVersion> _versions = new Dictionary<int, FunctionVersion>();
        readonly string _folder;

        public Function(AppManager appManager, string folder)
        {
            _appManager = appManager;
            _folder = folder;
            Directory.CreateDirectory(_folder);

            LoadExistingVersions();
        }

        void LoadExistingVersions()
        {
            foreach (string versionFolderPath in Directory.EnumerateDirectories(_folder))
            {
                if (!Int32.TryParse(Path.GetFileName(versionFolderPath), out int version))
                {
                    // Ignore non-version folders
                    continue;
                }

                string packagePath = Path.Combine(versionFolderPath, ZipFileName);
                _versions[version] = new FunctionVersion(_appManager, packagePath);
            }
        }

        public async Task CreateNewVersion(Stream zipContent)
        {
            int newVersion = GetNewVersion();

            string versionFolder = Path.Combine(_folder, newVersion.ToString());
            Directory.CreateDirectory(versionFolder);
            string packagePath = Path.Combine(versionFolder, ZipFileName);
            using (Stream zip = File.Create(packagePath))
            {
                await zipContent.CopyToAsync(zip);
            }

            _versions[newVersion] = new FunctionVersion(_appManager, packagePath);
        }

        public async Task<dynamic> Run(JObject requestBody, int? version)
        {
            var funcVersion = GetFunctionVersion(version);
            return await funcVersion.Run(requestBody);
        }

        FunctionVersion GetFunctionVersion(int? version = null)
        {
            int versionToRun = version != null ? version.Value : GetLatestVersion();
            if (versionToRun <= 0)
            {
                throw new FileNotFoundException($"Function doesn't have any versions");
            }

            if (!_versions.TryGetValue(versionToRun, out FunctionVersion funcVersion))
            {
                throw new FileNotFoundException($"Version {versionToRun} doesn't exist");
            }

            return funcVersion;
        }

        public IEnumerable<int> GetVersions()
        {
            return _versions.Keys.OrderBy(n => n);
        }

        int GetLatestVersion()
        {
            return _versions.Keys.OrderByDescending(v => v).FirstOrDefault();
        }

        int GetNewVersion()
        {
            return GetLatestVersion() + 1;
        }

        public async Task Delete()
        {
            foreach(int version in _versions.Keys.ToList())
            {
                await DeleteVersion(version);
            }

            Directory.Delete(_folder, recursive: true);
        }

        public async Task DeleteVersion(int version)
        {
            var funcVersion = GetFunctionVersion(version);
            await funcVersion.Delete();
            _versions.Remove(version);

            string versionFolderPath = Path.Combine(_folder, version.ToString());
            Directory.Delete(versionFolderPath, recursive: true);
        }
    }
}
