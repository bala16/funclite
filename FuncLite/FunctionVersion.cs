using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite
{
    public class FunctionVersion
    {
        readonly string _packagePath;

        public FunctionVersion(string packagePath)
        {
            _packagePath = packagePath;
        }
    }
}
