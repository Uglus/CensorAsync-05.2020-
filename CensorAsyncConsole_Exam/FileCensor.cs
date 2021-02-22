using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CensorAsyncConsole_Exam
{
    class FileCensor
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Size { get; set; }
        public int CountWords { get; set; }

        public FileCensor(string name, string path, string size)
        {
            Name = name;
            Path = path;
            Size = size;
            CountWords = 0;
        }
    }
}
