using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CensorAsyncConsole_Exam
{
    class Program
    {

        static Mutex mutex;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            List<FileInfo> listAllFiles = new List<FileInfo>();
            List<CensoredWord> listWordsCensored = new List<CensoredWord>();
            List<FileCensor> listFilesCensored = new List<FileCensor>();
            Thread threadSearch = new Thread(new ThreadStart(SearchFiles));

            try
            {
                mutex = Mutex.OpenExisting("Singleton");
            }
            catch (WaitHandleCannotBeOpenedException err){ }

            if (mutex != null)
            {
                Console.WriteLine("Програма вже запущена!");
                Console.WriteLine("Для виходу нажміть любу клавішу ...");
                Console.ReadKey();
                return;
            }

            using (mutex = new Mutex(false, "Singleton"))
            {
                InitBadWords();
                threadSearch.Start();



            }

            //////////////////////
            IEnumerable<string> GetText(string path)
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

             void SearchFiles()
            {
                Queue<string> queue = new Queue<string>();
                try
                {
                    DriveInfo[] allDrives = DriveInfo.GetDrives();
                    foreach (DriveInfo d in allDrives)
                    {
                        queue.Enqueue(d.Name);
                        foreach (string subDir in Directory.GetDirectories(d.Name))
                        {
                            queue.Enqueue(subDir);
                        }
                    }
                }
                catch (Exception ex) { }

                string path = "";
                while (queue.Count > 0)
                {
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
                            Console.Clear();
                            Console.WriteLine(files[i]);
                            Console.WriteLine();
                            Console.WriteLine("Знайдені файли з обраними словами:");
                            foreach (FileCensor fc in listFilesCensored)
                            {
                                Console.WriteLine($"Ім'я:{fc.Name} Шлях:{fc.Path} Розмір:{fc.Size} Кільскість замін:{fc.CountWords}\n");
                            }
                            Thread.Sleep(100); //Щоб в лейблі нормально відображались файли в яких мало слів


                            FileInfo fileInfo = new FileInfo(files[i]);
                            string fileName = fileInfo.Name;
                            string[] dirs = fileInfo.DirectoryName.Split('\\', ':');
                            string fileCopyName = $"({dirs[0]}-...-{dirs[dirs.Length - 1]})_{fileName}"; // $"{i}_{fileName}";
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


                                            badWord = true;
                                            textCensor += "******* ";
                                            swearWord.Count++;
                                            fileCensor.CountWords++;
                                        break;
                                    }
                                }

                                    if (badWord == false)
                                        textCensor += word + " ";
                                    badWord = false;
                            }

                            //якщо створений файл, то є вибране слово в цьому файлі
                            if (File.Exists(fileCopyPath))
                            {
                                FileStream fs = new FileStream(fileCopyPath, FileMode.Append);
                                byte[] bdata = Encoding.Default.GetBytes(textCensor);
                                fs.Write(bdata, 0, bdata.Length);
                                fs.Close();

                                listFilesCensored.Add(fileCensor);
                            }
                        }
                    }
                }

                WriteLog();
            }



            void InitBadWords()
            {
                Console.WriteLine("Введіть заборонені слова через пробіл");
                string[] words = Console.ReadLine().Split(' ');

                foreach (string word in words)
                {
                    listWordsCensored.Add(new CensoredWord(word));
                }
            }

             void WriteLog()
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
                foreach (CensoredWord cw in sortedWords)
                {
                    textLog += $"Слово: [{cw.Word}] Кількість замін: [{cw.Count}] \n";
                }

                textLog += "\n> Файли, в яких були ці слова:\n";
                foreach (FileCensor fc in listFilesCensored)
                {
                    textLog += $"Ім'я:{fc.Name} Шлях:{fc.Path} Розмір:{fc.Size} Кільскість замін:{fc.CountWords}\n";
                }
                textLog += "=====================================================\n\n\n";

                File.AppendAllText(pathLog, textLog);
            }



        }

         


    }
}
