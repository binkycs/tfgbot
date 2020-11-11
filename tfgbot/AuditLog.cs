using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace tfgbot
{
    class AuditLog
    {
        public static void AddLog(string text)
        {
            Console.WriteLine(text);
            File.AppendAllText("logs.txt", text + '\n');
        }
    }
}
