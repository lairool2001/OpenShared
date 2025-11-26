using Microsoft.VisualBasic;
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
    Console.WriteLine("完成");
    Console.ReadLine();
}
