using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;                      // OpenDialog
using System.Runtime.Remoting.Messaging;    // AsyncResult
using System.Windows.Threading;             // Timer
using System.Xml;
using System.Threading;
using System.IO;

namespace CrackerZIPArchiveWithPassword  //TODO: delete files, more entires, Enabled labels
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CreckerZIPPassword cracker;
        DispatcherTimer timer;
        DispatcherTimer timer2;

        OpenFileDialog OpenDialogZIPFile;
        TimeSpan time;
        string ArchiveName = "";
        SaveFileDialog SaveDialogState;
        IAsyncResult asyncResult;
        bool PasswordFound = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenDialogZIPFile = new OpenFileDialog();

            OpenDialogZIPFile.Filter = "ZIP-archive Files|*.zip";
            OpenDialogZIPFile.ShowDialog();
            labelFileName.Content = OpenDialogZIPFile.FileName;
            ArchiveName = OpenDialogZIPFile.FileName;

            if (!ArchiveName.Equals(""))
            {
                time = new TimeSpan(0, 0, 0);

                cracker = new CreckerZIPPassword();

                Func<string, string> thread = new Func<string, string>(cracker.GetPassword);

                timer = new DispatcherTimer();
                timer.Tick += Timer_Tick;
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Start();

                timer2 = new DispatcherTimer();
                timer2.Tick += Timer2_Tick;
                timer2.Interval = new TimeSpan(0, 0, 0, 0, 100);
                timer2.Start();
                
                asyncResult = thread.BeginInvoke(OpenDialogZIPFile.FileName, Callback, null);

                label1.Visibility = Visibility.Visible;
                label2.Visibility = Visibility.Visible;
                label3.Visibility = Visibility.Visible;
                MenuOpen.IsEnabled = false;
                MenuOpenState.IsEnabled = false;
                MenuSave.IsEnabled = true;
                MenuSaveAs.IsEnabled = true;
            }
        }

        private void MenuItemLoadState_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog passwordFile = new OpenFileDialog();
            passwordFile.Filter = "Password files|*.psw";
            passwordFile.ShowDialog();

            if (!passwordFile.FileName.Equals(""))
            {
                SaveDialogState = new SaveFileDialog(); // for quick save
                SaveDialogState.FileName = passwordFile.FileName;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(passwordFile.FileName);

                string password = "";
                int hours = 0;
                int minutes = 0;
                int seconds = 0;

                foreach (XmlNode task in xmlDoc.DocumentElement.ChildNodes)
                {
                    if (task.Name.Equals("fileName"))
                    {
                        ArchiveName = task.InnerText;
                        labelFileName.Content = ArchiveName;
                    }
                    else if (task.Name.Equals("password"))
                    {
                        password = task.InnerText;
                    }
                    else if (task.Name.Equals("hours"))
                    {
                        hours = Convert.ToInt32(task.InnerText);
                    }
                    else if (task.Name.Equals("minutes"))
                    {
                        minutes = Convert.ToInt32(task.InnerText);
                    }
                    else if (task.Name.Equals("seconds"))
                    {
                        seconds = Convert.ToInt32(task.InnerText);
                    }
                }

                FileInfo fi = new FileInfo(ArchiveName);
                if (!fi.Exists)
                {
                    MessageBox.Show("Archive not found, please choose file");

                    OpenDialogZIPFile = new OpenFileDialog();

                    OpenDialogZIPFile.Filter = "ZIP-archive Files|*.zip";
                    OpenDialogZIPFile.ShowDialog();
                    labelFileName.Content = OpenDialogZIPFile.FileName;
                    ArchiveName = OpenDialogZIPFile.FileName;
                }

                if(!string.IsNullOrEmpty(ArchiveName))
                {
                    time = new TimeSpan(hours, minutes, seconds);

                    cracker = new CreckerZIPPassword(password);

                    Func<string, string> thread = new Func<string, string>(cracker.GetPassword);

                    timer = new DispatcherTimer();
                    timer.Tick += Timer_Tick;
                    timer.Interval = new TimeSpan(0, 0, 1);
                    timer.Start();
                    timer2 = new DispatcherTimer();
                    timer2.Tick += Timer2_Tick;
                    timer2.Interval = new TimeSpan(0, 0, 0, 0, 100);
                    timer2.Start();

                    asyncResult = thread.BeginInvoke(ArchiveName, Callback, null);

                    label1.Visibility = Visibility.Visible;
                    label2.Visibility = Visibility.Visible;
                    label3.Visibility = Visibility.Visible;
                    MenuOpen.IsEnabled = false;
                    MenuOpenState.IsEnabled = false;
                    MenuSave.IsEnabled = true;
                    MenuSaveAs.IsEnabled = true;
                }
                
            }
        }

        private void Callback(IAsyncResult ar)
        {
            AsyncResult AR = ar as AsyncResult;
            Func<string, string> func = AR.AsyncDelegate as Func<string, string>;

            string s = func.EndInvoke(ar);
            Thread.Sleep(1100);
            timer.Stop();
            PasswordFound = true;
            MessageBox.Show("Password: " + s);
        }

        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            if (SaveDialogState == null || SaveDialogState.FileName.Equals(""))
            {
                MenuItemSaveAs_Click(null, null);
            }
            else
            {
                //создание XML файла
                //<?xml version = "1.0" encoding = "iso-8859-1"?>
                XmlTextWriter textWritter = new XmlTextWriter(SaveDialogState.FileName, Encoding.UTF8);
                textWritter.Formatting = Formatting.Indented;
                textWritter.IndentChar = '\t';
                textWritter.Indentation = 1;

                textWritter.WriteStartDocument();
                textWritter.WriteStartElement("cracker");

                textWritter.WriteStartElement("fileName");
                textWritter.WriteString(ArchiveName);
                textWritter.WriteEndElement();

                textWritter.WriteStartElement("password");
                textWritter.WriteString(cracker.CurrentPassword.ToString());
                textWritter.WriteEndElement();

                textWritter.WriteStartElement("hours");
                textWritter.WriteString(time.Hours.ToString());
                textWritter.WriteEndElement();

                textWritter.WriteStartElement("minutes");
                textWritter.WriteString(time.Minutes.ToString());
                textWritter.WriteEndElement();

                textWritter.WriteStartElement("seconds");
                textWritter.WriteString(time.Seconds.ToString());
                textWritter.WriteEndElement();

                textWritter.WriteEndElement();
                textWritter.Close();
            }
        }

        private void MenuItemSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveDialogState = new SaveFileDialog();
            SaveDialogState.Filter = "Password files|*.psw";
            SaveDialogState.ShowDialog();

            if (!SaveDialogState.FileName.Equals(""))
            {
                MenuItemSave_Click(null, null);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            time = time.Add(new TimeSpan(0, 0, 1));
            labelElapsedTime.Content = time.Hours.ToString() + "h: " + time.Minutes.ToString() + "m: " + time.Seconds.ToString() + "s";
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            labelCurrPassword.Content = cracker.CurrentPassword;
            if (asyncResult.IsCompleted)
            {
                MenuOpen.IsEnabled = true;
                MenuOpenState.IsEnabled = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (cracker != null && !PasswordFound)
            {
                MessageBoxResult res = MessageBox.Show("Do you want save state of search", "Exiting", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    MenuItemSave_Click(null, null);
                }

                try
                {
                    foreach (var item in cracker.listOfFile)
                    {
                        FileInfo Fi = new FileInfo(item);
                        Fi.Delete();
                    }
                    FileInfo F = new FileInfo("Destination.zip");
                    F.Delete();
                }
                catch (Exception)
                {
                }
            }
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
