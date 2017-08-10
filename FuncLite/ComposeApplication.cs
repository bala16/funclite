using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FuncLite
{
    public class ComposeApplication
    {
        private readonly string _applicationName;

        public string ApplicationName => $"fabric:/{_applicationName}";
        public string ComposeFileContent { get; }

        public ComposeApplication(string applicationName, string composeFile)
        {
            _applicationName = applicationName;
            ComposeFileContent = composeFile;
        }
    }
}