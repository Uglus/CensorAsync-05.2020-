using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CensorAsyncConsole_Exam
{
    class CensoredWord
    {
        public string Word { get; set; }
        public int Count { get; set; }

        public CensoredWord(string word)
        {
            Word = word;
            Count = 0;
        }
    }
}
