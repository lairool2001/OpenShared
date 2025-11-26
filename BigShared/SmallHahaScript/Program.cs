using System.Diagnostics;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
//get arg
if (args.Length > 0)
{
    string path = args[0];
    Console.WriteLine("Argument: " + path);
    string[] linez = File.ReadAllText(path).Replace("\r", "").Split("\n");
    for (int i = 0; i < linez.Length; i++)
    {
        string line = linez[i];
        string[] lineArgz = line.Split(' ');
        if (lineArgz.Length < 2) continue;
        string command = lineArgz[0];
        string data = lineArgz[1];
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(command);
        GoCommand(command, data);
    }
    Console.WriteLine("完成");
    Console.ReadLine();
}

static void GoCommand(string command, string data)
{
    try
    {
        switch (command)
        {
            case "執行":
                string fileName = data.Replace("\"", "");
                string exetype = Path.GetExtension(fileName).ToLower();
                string folder = Path.GetDirectoryName(fileName);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = fileName,
                    UseShellExecute = true,
                    WorkingDirectory = folder
                });
                break;
            case "等待秒":
                float waitTime = float.Parse(data);
                Thread.Sleep((int)(waitTime * 1000));
                break;
            case "命令台":
                while (true)
                {
                    Console.WriteLine("命令:");
                    string arg1 = Console.ReadLine();
                    if (arg1 == "結束") break;
                    Console.WriteLine("資料:");
                    string arg2 = Console.ReadLine();
                    try
                    {
                        GoCommand(arg1, arg2);
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("命令執行失敗");
                    }
                }
                break;
            default:
                break;
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Error executing command: " + ex.Message);
    }
}