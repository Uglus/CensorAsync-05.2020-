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

using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CensorAsync_Exam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<CensoredWord> listWordsCensored = new List<CensoredWord>();
        List<FileCensor> listFilesCensored = new List<FileCensor>();

        Thread threadSearch;

        private Mutex mutex = new Mutex(false, "CensorAsync_Exam");

        public MainWindow()
        {
            if (!mutex.WaitOne(500, false))
            {
                MessageBox.Show("Програма вже запущена!", "Помилка");
                this.Close();
                return;
            }

            InitializeComponent();

            progressStatus.Maximum = 1;
            progressStatus.Value = 0;

            //Ініціалізація комбобоксу
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                comboDisks.Items.Add(d);
                comboDisks.DisplayMemberPath = Name;
            }
            comboDisks.Items.Add("Всі диски");
        }

        private async void BtnStartSearch_Click(object sender, RoutedEventArgs e)
        {
            threadSearch = new Thread(new ThreadStart(SearchFiles));
            btnStopSearch.IsEnabled = true;
            btnBreakSearch.IsEnabled = true;
            btnStartSearch.IsEnabled = false;
            btnResumeSearch.IsEnabled = false;

            progressStatus.Maximum = 0;
            progressStatus.Value = 0;

            filesListView.Items.Clear();

            await Task.Run(() =>
            {
                InitBadWords();
            });

            await Task.Run(() =>
            {
                threadSearch.Start();
            });
        }

        private async void BtnResumeSearch_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                threadSearch.Resume();  
            });

            btnResumeSearch.IsEnabled = false;
            btnStopSearch.IsEnabled = true;
            btnBreakSearch.IsEnabled = true;
        }

        private async void BtnStopSearch_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                threadSearch.Suspend();
            });

            btnStopSearch.IsEnabled = false;
            btnResumeSearch.IsEnabled = true;
            btnBreakSearch.IsEnabled = false;
        }


        private async void BtnBreakSearch_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                threadSearch.Abort();
            });
            btnStartSearch.IsEnabled = true;
            btnStopSearch.IsEnabled = false;
            btnResumeSearch.IsEnabled = false;
            btnBreakSearch.IsEnabled = false;

            progressStatus.Maximum = 1;
            progressStatus.Value = 0;

            labelInfo.Content = "Операція закінчена";

            WriteLog();

        }

        static IEnumerable<string> GetText(string path)
        {
            string filesText = null;
            string[] words = null;
            try
            {
                filesText = File.ReadAllText(path);
                words = filesText.Split(' ');
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            if (words != null)
            {
                for (int i = 0; i < words.Length; i++)
                {
                    yield return words[i];
                }
            }
        }

        private void SearchFiles()
        {
            Queue<string> queue = new Queue<string>();
            try
            {
                List<DriveInfo> allDrives = new List<DriveInfo>();
                this.Dispatcher.Invoke((Action)delegate
                {
                    if (comboDisks.SelectedValue.ToString() != "Всі диски")
                        allDrives.Add((DriveInfo)comboDisks.SelectedItem);
                    else
                    {
                        foreach (DriveInfo d in DriveInfo.GetDrives())
                        {
                            allDrives.Add(d);
                        }
                    }
                });

                foreach (DriveInfo d in allDrives)
                {
                    this.Dispatcher.Invoke((Action)delegate
                    { progressStatus.Maximum += Directory.GetDirectories(d.Name).Count(); });

                    queue.Enqueue(d.Name);
                    foreach (string subDir in Directory.GetDirectories(d.Name))
                    {
                        queue.Enqueue(subDir);
                    }
                }
            }
            catch (Exception ex) {}

            string path = "";
            while (queue.Count > 0)
            {
                this.Dispatcher.Invoke((Action)delegate
                { progressStatus.Value++; });

                path = queue.Dequeue();
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories); // SearchOption.AllDirectories | SearchOption.TopDirectoryOnly
                }
                catch (Exception ex) { }

                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        this.Dispatcher.Invoke((Action)delegate
                        {
                            this.labelInfo.Content = files[i];
                        });
                        Thread.Sleep(50); //Щоб в лейблі нормально відображались файли в яких мало слів

                        FileInfo fileInfo = new FileInfo(files[i]);
                        string fileName = fileInfo.Name;
                        string[] dirs = fileInfo.DirectoryName.Split('\\', ':');
                        string fileCopyName = $"({dirs[0]}-...-{dirs[dirs.Length - 1]})_{fileName}";// $"{i}_{fileName}";// 
                        string fileCopyPath = $@"..\..\Data\{fileCopyName}";

                        string textCensor = null;
                        bool badWord = false;
                        FileCensor fileCensor = new FileCensor(fileInfo.Name, fileInfo.DirectoryName, fileInfo.Length.ToString() + " bytes");
                        foreach (string word in GetText(files[i]))
                        {
                            foreach (CensoredWord swearWord in listWordsCensored)
                            {
                                if (word == swearWord.Word)
                                {
                                    if (!File.Exists(fileCopyPath))
                                    {
                                        FileStream fs = new FileStream(fileCopyPath, FileMode.CreateNew);
                                        fs.Close();
                                    }

                                    lock (this) //Синхронізація
                                    {
                                        badWord = true;
                                        textCensor += "******* ";
                                        swearWord.Count++; 
                                        fileCensor.CountWords++;
                                    }
                                    break;
                                }
                            }

                            lock (this) //Синхронізація
                            {
                                if (badWord == false)
                                    textCensor += word + " ";
                                badWord = false;
                            }
                        }

                        //якщо створений файл, то є вибране слово в цьому файлі
                        if (File.Exists(fileCopyPath))
                        {
                            FileStream fs = new FileStream(fileCopyPath, FileMode.Append);
                            byte[] bdata = Encoding.Default.GetBytes(textCensor);
                            fs.Write(bdata, 0, bdata.Length);
                            fs.Close();

                            listFilesCensored.Add(fileCensor);

                            this.Dispatcher.Invoke((Action)delegate
                            { filesListView.Items.Add(fileCensor); });
                        }
                    }
                }
            }

            this.Dispatcher.Invoke((Action)delegate
            {this.labelInfo.Content = "Сканування завершено";
                this.btnStartSearch.IsEnabled = true;
                this.btnStopSearch.IsEnabled = false;
                this.btnResumeSearch.IsEnabled = false;
                this.btnBreakSearch.IsEnabled = false;

                this.progressStatus.Maximum = 1;
                this.progressStatus.Value = 0;
            });

            WriteLog();

        }

        void InitBadWords()
        {
            string[] words = null;
            this.Dispatcher.Invoke((Action)delegate
            {
                words = boxWords.Text.Split(' ');
            });
            foreach(string word in words)
            {
                listWordsCensored.Add(new CensoredWord(word));
            }
        }

        private void WriteLog()
        {
            string pathLog = @"..\..\Data\log.txt";
            if (!File.Exists(pathLog))
            {
                FileStream fs = new FileStream(pathLog, FileMode.Create);
                fs.Close();
            }

            string textLog = "";
            //Для сортування по популярності слова
            var sortedWords = from t in listWordsCensored
                                orderby t.Count descending
                                select t;
            textLog += $"Дата операції: {DateTime.Now}";
            textLog += "\n> Загальна статистика по словам: \n";
            foreach(CensoredWord cw in sortedWords)
            {
                textLog += $"Слово: [{cw.Word}] Кількість замін: [{cw.Count}] \n";
            }

            textLog += "\n> Файли, в яких були ці слова:\n";
            foreach(FileCensor fc in listFilesCensored)
            {
                textLog += $"Ім'я:{fc.Name} Шлях:{fc.Path} Розмір:{fc.Size} Кільскість замін:{fc.CountWords}\n";
            }
            textLog += "=====================================================\n\n\n";

            File.AppendAllText(pathLog, textLog);
        }
        
    }
}
