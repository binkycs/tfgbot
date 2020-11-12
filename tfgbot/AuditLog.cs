using System;
using System.IO;
using System.Threading.Tasks;

namespace tfgbot
{
    class AuditLog
    {
        public static async void AddLog(string text)
        {
            string time = DateTime.Now.ToString("G");
            string logText = time + ": " + text;

            await Task.Run(() =>
            {
                using (var stream = new FileStream("audit.log", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write, 4096, useAsync: true))
                {
                    StreamWriter writer = new StreamWriter(stream);
                    long end = stream.Length;

                    stream.Seek(end, SeekOrigin.Begin);
                    writer.WriteLine(logText);
                    writer.Flush();
                }
            });
        }
    }
}
