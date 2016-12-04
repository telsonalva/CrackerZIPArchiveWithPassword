using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using SharpCompress.Archive.Zip;
using System.Windows;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Concurrent;

namespace CrackerZIPArchiveWithPassword
{
    class CreckerZIPPassword
    {
        object LockObject = new object();
        object LockObject2 = new object();
        public List<string> listOfFile = new List<string>();          // for deleting files after close programm
        private string currentPassword;
        private string CorrectPassword;
        public string CurrentPassword
        {
            get
            {
                //lock (LockObject)
                //{
                //    return currentPassword;
                //}
                return currentPassword;
            }
            set
            {
                //lock (LockObject)
                //{
                //    currentPassword = value;
                //}
                currentPassword = value;
            }
        }
        private bool passwordFound;
        public bool PasswordFound
        {
            get
            {
                //lock (LockObject)
                //{
                //    return passwordFound;
                //}
                return passwordFound;

            }
            set
            {
                //lock (LockObject)
                //{
                //    passwordFound = value;
                //}
                passwordFound = value;
            }
        }
        public int _noofthreads;
        public int NoOfThreads
        {
            get
            {
                lock (LockObject2)
                {
                    return _noofthreads;
                }

            }
            set
            {
                lock (LockObject2)
                {
                    _noofthreads = value;
                }
            }
        }

        public int NoOfPasswords;
        public List<string> ListOfPasswords = new List<string>(); // create a batch of passwords
        public int passwords_per_batch = 100000; // this is to set a batch limit
        ZipArchive ZIPArchive;
        Stream stream;
        FileStream writer;
        public int countAtemptsCompareCRC = 0;
        Log _log = new Log();

        public CreckerZIPPassword()
        {
            currentPassword = "0";
            CorrectPassword = "";
        }

        public CreckerZIPPassword(string currentPassword)
        {
            this.currentPassword = currentPassword;
            this.CorrectPassword = null;
        }

        public string GetPassword(string pathOfZipArchive,bool IsMultiThreading)
        {
            bool passwordIsFine = false;
            uint CRCOfEntry = 0;
            string nameOfEntryInArchive = "";

            if (!IsMultiThreading)
            {
                #region SingleThread
                while (!passwordIsFine)
                {
                    //first create a zip object with the necessary parameters
                    ZIPArchive = ZipArchive.Open(pathOfZipArchive, currentPassword);
                    NoOfThreads = Process.GetCurrentProcess().Threads.Count;
                    NoOfPasswords++;
                    try
                    {
                        //now try to read each entry of this zip object, if password protected, reading these entries will throw an exception
                        foreach (var entry in ZIPArchive.Entries)
                        {
                            stream = entry.OpenEntryStream();//will throw an exception if password is not correct
                             /*
                              * The next logic was created by the original developer, will need to review
                              * if password is correct then extract each item of the archive and get its CRC value
                              * this CRC value will be cross checked later whilst re-zipping the archive
                              * the CRC check is because at times .OpenEntryStream() method does not throw a password exception.
                              */                               
                            nameOfEntryInArchive = entry.FilePath;
                            if (!listOfFile.Contains(entry.FilePath))
                            {
                                listOfFile.Add(entry.FilePath);
                            }
                            writer = new FileStream(nameOfEntryInArchive, FileMode.Create, FileAccess.Write);
                            entry.WriteTo(writer);
                            CRCOfEntry = entry.Crc;
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
                    //a new archive is created and the extracted files are zipped back
                    using (var newArchive = ZipArchive.Create())
                    {
                        newArchive.AddEntry(nameOfEntryInArchive, new FileInfo(nameOfEntryInArchive));

                        using (Stream newStream = File.Create("Destination.zip"))
                        {
                            newArchive.SaveTo(newStream, SharpCompress.Common.CompressionType.LZMA);
                        }

                        //re-opening the new archive and check each item for its CRC check
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
                    passwordIsFine = true;
                }
                #endregion

                //MessageBox.Show("No Of Passwords : " + noofpasswords.ToString());
                return CurrentPassword;
            }
            else
            {
                #region MultipleThreads
                PasswordFound = false;
                int noofpasswords = 0;
                int noofpasswords1 = 0;
                int noofpasswords2 = 0;

                while (!PasswordFound)
                {
                    //the password list when accessed the first time will have zero items, hence will start the generation from 'currentpassword'
                    //however even after iterating through all the items in this list, if the password is not found, then we must populate the next batch
                    //to populate the next batch, we must remember the last item of the password list.
                    
                    //if the list has items, then it has completed atleast one batch
                    if(ListOfPasswords.Count>0)
                    {
                        //set the last item of the list as the currentpassword so that the subsequent batches can be generated appropriately
                        CurrentPassword = ListOfPasswords[passwords_per_batch - 1];
                    }
                    PopulatePasswords();
                    //using Parallel.ForEach in a simple iteration like this wont improve performance, neither will .AsParallel()

                    // set the threads
                    //int num_threads = 5;
                    //Thread[] threads = new Thread[num_threads];
                    List<Thread> workerThreads = new List<Thread>();

                    //split the list into two

                    Thread thread1 = new Thread(() =>
                    {
                        for (int x = 0; x < 49999; x++)
                        {
                            noofpasswords1++;
                            NoOfPasswords = noofpasswords1 + noofpasswords2;
                            bool _tmp = ParallelProcessingArchive(ListOfPasswords[x], pathOfZipArchive);
                            if (PasswordFound)
                            {
                                break;
                            }
                            else
                            {
                                PasswordFound = _tmp;
                            }
                        }
                    });

                    Thread thread2 = new Thread(() =>
                    {
                        for (int x = 50000; x < 99999; x++)
                        {
                            noofpasswords2++;
                            NoOfPasswords = noofpasswords1 + noofpasswords2;
                            bool _tmp = ParallelProcessingArchive(ListOfPasswords[x], pathOfZipArchive);
                            if (PasswordFound)
                            {
                                break;
                            }
                            else
                            {
                                PasswordFound = _tmp;
                            }
                        }
                    });

                    workerThreads.Add(thread1);
                    workerThreads.Add(thread2);
                    thread1.Start();
                    thread2.Start();

                    // wait for all threads to finish
                    foreach (Thread thread in workerThreads)
                    {
                        thread.Join();
                    }

                    NoOfPasswords = noofpasswords1 + noofpasswords2;
                    //MessageBox.Show("No Of Passwords : " + noofpasswords.ToString());

                    //for (int x = 0; x < 499; x++)
                    //{
                    //    for (int i = 0; i < (num_threads - 1); i++)
                    //    {
                    //        threads[i] = new Thread(() =>
                    //        {
                    //            PasswordFound = ParallelProcessingArchive(ListOfPasswords[x + i], pathOfZipArchive);
                    //        });
                    //        threads[i].Start();

                    //    }

                    //    // wait all threads to finish
                    //    for (int i = 0; i < (num_threads - 1); i++)
                    //    {
                    //        threads[i].Join();
                    //    }


                    //    string first = ListOfPasswords[x];
                    //    string second = ListOfPasswords[x + 1];
                    //    string third = ListOfPasswords[x + 2];
                    //    string fourth = ListOfPasswords[x + 3];
                    //    string fifth = ListOfPasswords[x + 4];

                    //}


                    //Partition the entire source array.
                    //var rangePartitioner = Partitioner.Create(0, ListOfPasswords.Count);
                    // Parallel.ForEach(rangePartitioner, (range, state) =>
                    //  {
                    //      // Loop over each range element without a delegate invocation.
                    //      for (int i = range.Item1; i < range.Item2; i++)
                    //      {
                    //          PasswordFound = ParallelProcessingArchive(ListOfPasswords[i], pathOfZipArchive);
                    //          if (PasswordFound)
                    //          {
                    //              break;
                    //          }
                    //      }
                    //      if (PasswordFound)
                    //          state.Stop();
                    //  });

                    //Parallel.ForEach(ListOfPasswords, (password, state) =>
                    // {
                    //     PasswordFound = ParallelProcessingArchive(password, pathOfZipArchive);
                    //     if (PasswordFound)
                    //         state.Stop();
                    // });

                    //ListOfPasswords.AsParallel().ForAll(password => PasswordFound = ParallelProcessingArchive(password, pathOfZipArchive));

                }

                #endregion

                return CorrectPassword;
            }
            
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

        private void PopulatePasswords()
        {
            ListOfPasswords.Clear();
            for(int i=0;i<passwords_per_batch;i++)
            {
                ListOfPasswords.Add(CurrentPassword);
                
                IncrementPassword(CurrentPassword.Length - 1);
            }
            string _tempfolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\\" + Process.GetCurrentProcess().ProcessName;
            File.WriteAllLines(_tempfolder + @"\Passwords.txt",ListOfPasswords.ToArray());
        }

        private void DeleteFilesAndFolders(List<string> _list)
        {
            try
            {
                //delete files
                foreach (string s in _list)
                {
                    if (File.Exists(s))
                        File.Delete(s);

                    string _dir = Path.GetDirectoryName(s);
                    if(Directory.Exists(_dir))
                        Directory.Delete(_dir, true);
                }
            }
            catch (Exception ex)
            {
                _log.write(ex);
            }
        }

        private bool ParallelProcessingArchive(string password,string pathOfZipArchive)
        {
            NoOfThreads = Process.GetCurrentProcess().Threads.Count;
            CurrentPassword = password;
            List<string> _listOfFile = new List<string>();
            uint _CRCOfEntry = 0;
            string _nameOfEntryInArchive = null;
            //string _tempfolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+ @"\\" + Process.GetCurrentProcess().ProcessName + @"\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + @"\";
            string _tempfolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\\" + Process.GetCurrentProcess().ProcessName + @"\\" + Thread.CurrentThread.ManagedThreadId + @"\";
            Directory.CreateDirectory(_tempfolder);
            //string _tempsrcarchive = _tempfolder + "archive.zip";
            //if (!File.Exists(_tempsrcarchive))
            //    File.Copy(pathOfZipArchive, _tempsrcarchive, true);
            using (ZipArchive _ziparchive = ZipArchive.Open(pathOfZipArchive, password))
            {
                if (_ziparchive.Entries.Count > 0)
                {
                    int entrycount = 0;
                    foreach (var entry in _ziparchive.Entries)
                    {
                        entrycount++;
                        if (!(entrycount > 1)) //only check the first entry
                        {
                            try
                            {
                                Stream _stream = entry.OpenEntryStream(); //sometimes throws a password did not match exception, sometimes it doesn't
                                _nameOfEntryInArchive = entry.FilePath;

                                
                                if (!_listOfFile.Contains(_tempfolder + _nameOfEntryInArchive))
                                {
                                    _listOfFile.Add(_tempfolder + _nameOfEntryInArchive);
                                }
                                try
                                {
                                    using (FileStream _writer = new FileStream(_tempfolder + _nameOfEntryInArchive, FileMode.Create, FileAccess.Write))
                                    {
                                        entry.WriteTo(_writer); // will throw a 'Bad state (oversubscribed dynamic bit lengths tree)' error if password doesnt match or file corrupted.
                                        _CRCOfEntry = entry.Crc;
                                    }

                                    uint _destCRC = 0;
                                    //a new archive is created and the extracted files are zipped back
                                    using (var newArchive = ZipArchive.Create())
                                    {
                                        newArchive.AddEntry(_tempfolder + _nameOfEntryInArchive, new FileInfo(_tempfolder + _nameOfEntryInArchive));

                                        using (Stream newStream = File.Create(_tempfolder + "Destination.zip"))
                                        {
                                            newArchive.SaveTo(newStream, SharpCompress.Common.CompressionType.LZMA);
                                        }

                                        //re-opening the new archive and check each item for its CRC check
                                        ZipArchive z = ZipArchive.Open(_tempfolder + "Destination.zip");

                                        foreach (var item in z.Entries)
                                        {
                                            _destCRC = item.Crc;
                                        }
                                        z.Dispose();
                                    }

                                    if (_CRCOfEntry != _destCRC)
                                    {
                                        //DeleteFilesAndFolders(_listOfFile);
                                        countAtemptsCompareCRC++;
                                    }
                                    else
                                    {
                                        //DeleteFilesAndFolders(_listOfFile);
                                        CorrectPassword = password;
                                        return true;
                                    }


                                }
                                catch (Exception ex)
                                {
                                    string _msg1 = "Bad state";
                                    string errormsg = ex.Message;

                                    //DeleteFilesAndFolders(_listOfFile);
                                    if (!errormsg.Contains(_msg1))
                                    {
                                        CorrectPassword = errormsg;
                                        _log.write(ex);

                                        return true;
                                    }
                                }


                            }
                            catch (Exception ex)
                            {
                                string _msg1 = "The password did not match.";
                                string errormsg = ex.Message;

                                if (!errormsg.Equals(_msg1))
                                {
                                    CorrectPassword = errormsg;
                                    _log.write(ex);

                                    return true;
                                }
                                continue;
                            }
                        }
                    }

                }

            }
            return false;
        }
    }
}
