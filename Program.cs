using Gma.System.MouseKeyHook;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

class Program
{
    static  List<string> words = new List<string>();
    static char[] symb = { ' ', ',', '.', ';', ':', '-', '!', '?' };
    static string path;
    static string FileName;
    static string pathFile;
    static List<string> ProgName = new List<string>();
    static List<string> KeyDownInfo = new List<string>();
    static int timeMonitiring = 10;

    static bool EnableKeyLogging;
    static bool EnableProcessMonitoring;

    static List<string> ProcessInfo = new List<string>();

    static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

    static void Main(string[] args)
    {
        Menu();
    }

    static void Menu()
    {
        bool flag = true;
        while (flag)
        {
            Console.Clear();
            Console.WriteLine("Выберите режим:");
            int action;


            Console.WriteLine("1 - Первый режим (позволяет  настроить опции для слежения)");
            Console.WriteLine("2 - Второй режим (выполняет процесс слежения)");
            Console.WriteLine("3 - Третий режим (позволяет посмотреть отчет о работе программы)");
            Console.WriteLine("0 - выход");



            Console.Write("действие - ");
            while (!Int32.TryParse(Console.ReadLine(), out action) || action < 0 || action > 3)
            {
                Console.WriteLine("Не верный ввод.Введите число:");
                Console.Write("действие - ");
            }
            Thread.Sleep(100);
            Console.Clear();


            switch (action)
            {
                case 0:
                    flag = false;
                    break;
                case 1:
                    
                    FirstMode();
                    
                    break;
                case 2:
                    
                    SecondMode(cancelTokenSource.Token);
                    
                    break;
                case 3:
                    
                    ViewReport();
                    Console.WriteLine("--------------------------------");
                    Console.WriteLine("Нажмите Enter для выхода в меню ");
                    Console.ReadLine();
                    

                    break;
            }

        }
    }


    static void FirstMode()
    {
        Console.WriteLine("Введите список слов (для создания файл отчета");
        words = Console.ReadLine().Split(symb).ToList();

        Console.WriteLine("Укажите путь к папке, где желаете создать файл отчета");
        path = Console.ReadLine();

        Console.WriteLine("Укажите название файла отчета");
        FileName = Console.ReadLine();

        Console.WriteLine("Укажите список запрещенных программ");
        ProgName = Console.ReadLine().Split(symb).ToList();

       
    }

    static void SecondMode(CancellationToken cancellationToken)
    {
        Console.WriteLine("Ввведите слово");
        string word = Console.ReadLine();

        Console.WriteLine("Укажите время мониторинга (сек)");
        while (!Int32.TryParse(Console.ReadLine(), out timeMonitiring) || timeMonitiring < 0)
        {
            Console.WriteLine("Не верный ввод.Введите число:");
            Console.Write("время мониторинга (сек) - ");
        }
        bool flag = false;
        if (words.Any(w => words.Contains(word)))
        {
            pathFile = path + "\\" + $"{FileName}.txt";
            FileReportInfo(cancellationToken);
        }
    }


    static async void FileReportInfo(CancellationToken cancellationToken)
    {
        
        var ProcessInfoTask = Task.Run(() => MonitorProcessInfo(cancellationToken));
        var KeyDownInfoTask = Task.Run(() => MonitorKeyDownInfo(cancellationToken));


        cancelTokenSource.CancelAfter(TimeSpan.FromSeconds(timeMonitiring));

        ProcessInfoTask.Wait();
        KeyDownInfoTask.Wait();

        var ProcessInfo = ProcessInfoTask.Result;
        var KeyDownInfo = KeyDownInfoTask.Result;

        AddToFile(ProcessInfo, "Запущен процесс");
       
        AddToFile(KeyDownInfo, "Нажата кнопка");

        Console.WriteLine("Мониторинг запущенных процессов и нажатий клаишь завершен.");
    }

    static void AddToFile(List<string> lines, string msg)
    {
        if (!File.Exists(pathFile))
        {
            var stream = File.Create(pathFile);
            stream.Close();
        }

        using (var writer = new StreamWriter(pathFile, append: true))
        {
            
            foreach (var line in lines)
                writer.WriteLine($"{msg}: {line}");
        }
    }

    static async Task<List<string>> MonitorProcessInfo(CancellationToken cancellationToken)
    {

        while (!cancellationToken.IsCancellationRequested)
        {
            var processes = Process.GetProcesses();
            for (var i = processes.Length - 1; i >=0 ; i--)
            {
                foreach (var procName in ProgName)
                {
                    if (processes[i].ProcessName.ToLower() == procName.ToLower())
                    {
                        string ReportInfo = $"Запущено приложение {procName} в {DateTime.Now}";
                        ProcessInfo.Add(ReportInfo);
                        processes[i].Kill();
                        ReportInfo = $"Приложение {procName} из списка запрещенных, закрыто в {DateTime.Now}";
                        ProcessInfo.Add(ReportInfo);
                    }
                    if (ProcessInfo.Count < 50)
                    {
                        var ReportInfo2 = $"Запущено приложение {processes[i].ProcessName} в {DateTime.Now}";
                        ProcessInfo.Add(ReportInfo2);
                    }

                }
            }
            Thread.Sleep(1000);
        }
        return ProcessInfo;
    }

    static async Task<List<string>> MonitorKeyDownInfo(CancellationToken cancellationToken)
    {

        while (!cancellationToken.IsCancellationRequested)
        {
            if(Console.KeyAvailable)
            {
                KeyDownInfo.Add( Console.ReadKey().KeyChar.ToString());
            }
        }
        return KeyDownInfo;
    }



    static void ViewReport()
    {
        Console.WriteLine("Отчет:");
        if (File.Exists(pathFile))
        {
            var report = File.ReadAllLines(pathFile);
            foreach (var line in report)
            {
                Console.WriteLine(line);
            }
        }
        else
        {
            Console.WriteLine("Отчет не найден.");
        }
    }
}


//static async Task StartMonitoring()
//{
//    var cts = new CancellationTokenSource();
//    var monitoringTask = Task.Run(() => Monitor(cts.Token));

//    Console.WriteLine("Нажмите Enter для остановки мониторинга...");
//    Console.ReadLine();
//    cts.Cancel();
//    await monitoringTask;
//}

//static async Task Monitor(CancellationToken cancellationToken)
//{
//    if (settings.EnableKeyLogging)
//    {
//        keyLogger.StartLogging(settings.ReportPath, cancellationToken);
//    }

//    if (settings.EnableProcessMonitoring)
//    {
//        processWatcher.StartMonitoring(settings.ReportPath, cancellationToken);
//    }


//    while (!cancellationToken.IsCancellationRequested)
//    {
//        await Task.Delay(1000);
//    }
//}

















