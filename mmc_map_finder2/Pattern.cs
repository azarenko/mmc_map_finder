using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mmc_map_finder2
{
    internal class Pattern
    {
        public byte[] Header { get; set; }
        public int HeaderLenght;
        public int PayloadLenght { get; set; }
    }
}
