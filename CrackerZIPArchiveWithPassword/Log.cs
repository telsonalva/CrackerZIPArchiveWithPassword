using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrackerZIPArchiveWithPassword
{
    class Log
    {
        string _logfile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\\" + Process.GetCurrentProcess().ProcessName + @"\\Logs\ErrorLog.txt";
        public void write(Exception ex)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logfile));
            FileStream _filestream = new FileStream(_logfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using (StreamWriter _streamwriter = new StreamWriter(_filestream))
            {
                _streamwriter.WriteLine(" *** ERROR ENTRY ***");
                _streamwriter.WriteLine("Time Stamp : " + DateTime.Now.ToString());
                _streamwriter.WriteLine(ex.ToString());
                _streamwriter.WriteLine("-------------------------------------------");
            }
        }
    }
}
