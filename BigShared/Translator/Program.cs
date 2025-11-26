using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

internal class data
{
    private const string ApiKey = "";
    private const string EndpointUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro:generateContent";
    
    internal static string Main(string[] argz)
    {
        string[] translatez = new string[argz.Length];
        for (int i = 0; i < argz.Length; i++)
        {
            translatez[i] = translate(argz[i]);
        }
        string jsonString = JsonSerializer.Serialize(translatez);
        return jsonString;
    }
    static void test()
    {
        while (true)
        {
            Console.WriteLine("enter:");
            string input = Console.ReadLine();
            Console.WriteLine("translate:");
            Console.ReadLine();
        }
    }
    static string translate(string text)
    {
        return "";
    }
    //lairool2010@gmail.com
}
