using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite
{
    public class ACIApp
    {
        public readonly string _containerGroup;
        public readonly uint _port;

        public ACIApp(string containerGroup)
        {
            _containerGroup = containerGroup;
            _port = 1337;
        }
    }
}
