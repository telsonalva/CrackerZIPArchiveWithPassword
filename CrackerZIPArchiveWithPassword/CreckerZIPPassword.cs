using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using SharpCompress.Archive.Zip;
using System.Windows;


namespace CrackerZIPArchiveWithPassword
{
    class CreckerZIPPassword
    {
        object LockObject = new object();
        public List<string> listOfFile = new List<string>();          // for deleting files after close programm
        private string currentPassword;
        public string CurrentPassword
        {
            get
            {
                lock (LockObject)
                {
                    return currentPassword;
                }

            }
            set
            {
                lock (LockObject)
                {
                    currentPassword = value;
                }
            }
        }


        ZipArchive ZIPArchive;
        Stream stream;
        FileStream writer;
        public int countAtemptsCompareCRC = 0;

        public CreckerZIPPassword()
        {
            currentPassword = "0";
        }

        public CreckerZIPPassword(string currentPassword)
        {
            this.currentPassword = currentPassword;
        }

        public string GetPassword(string pathOfZipArchive)
        {
            bool passwordIsFind = false;
            uint CRCOfEntry = 0;
            string nameOfEntireInArchive = "";
            
            while (!passwordIsFind)
            {
                try
                {
                    ZIPArchive = ZipArchive.Open(pathOfZipArchive, currentPassword);
                }
                catch (Exception e)
                {
                    MessageBox.Show("File not found or you input wrong name of file");
                    break;
                }

                try
                {
                    foreach (var entire in ZIPArchive.Entries)
                    {
                        stream = entire.OpenEntryStream();
                        nameOfEntireInArchive = entire.FilePath;
                        if (!listOfFile.Contains(entire.FilePath))    
                        {
                            listOfFile.Add(entire.FilePath); 
                        }        
                        writer = new FileStream(nameOfEntireInArchive, FileMode.Create, FileAccess.Write);
                        entire.WriteTo(writer);
                        CRCOfEntry = entire.Crc;
                    }
                }
                catch (Exception ex)
                {
                    if (ZIPArchive != null)
                    {
                        ZIPArchive.Dispose();
                    }
                    if (stream != null)
                    {
                        stream.Close();
                    }
                    if (writer != null)
                    {
                        writer.Close();
                    }
                    
                    IncrementPassword(CurrentPassword.Length - 1);
                    continue;
                }

                writer.Close();
                uint destCRC = 0;
                using (var newArchive = ZipArchive.Create())
                {
                    newArchive.AddEntry(nameOfEntireInArchive, new FileInfo(nameOfEntireInArchive));

                    using (Stream newStream = File.Create("Destination.zip"))
                    {
                        newArchive.SaveTo(newStream, SharpCompress.Common.CompressionType.LZMA);
                    }

                    ZipArchive z = ZipArchive.Open("Destination.zip");
                   
                    foreach (var item in z.Entries)
                    {
                        destCRC = item.Crc;
                    }
                    z.Dispose();
                }

                if (CRCOfEntry != destCRC)
                {
                    countAtemptsCompareCRC++;
                    IncrementPassword(CurrentPassword.Length - 1);
                   
                    continue;
                }
                //MessageBox.Show("Password: " + CurrentPassword);
                passwordIsFind = true;
            }
            return CurrentPassword;
        }

        private void IncrementPassword(int position)
        {
            if (CurrentPassword[position].Equals('9'))
            {
                PasteCharInPassword(position, 'a');
                return;
            }
            if (CurrentPassword[position].Equals('z'))
            {
                PasteCharInPassword(position, 'A');
                return;
            }
            if (CurrentPassword[position].Equals('Z'))
            {
                if (position == 0)
                {
                    string str = CurrentPassword.Insert(position, '0'.ToString());
                    CurrentPassword = str;
                    PasteCharInPassword(position + 1, '0');
                    return;
                }
                else
                {
                    PasteCharInPassword(position, '0');
                    IncrementPassword(position - 1);
                }
            }
            else
            {
                char symb = CurrentPassword[position];
                symb++;
                PasteCharInPassword(position, symb);
            }
        }

        private void PasteCharInPassword(int position, char symbol)
        {
            string string1 = CurrentPassword.Insert(position, symbol.ToString());
            string string2 = string1.Remove(position + 1, 1);
            CurrentPassword = string2;
        }
    }
}
