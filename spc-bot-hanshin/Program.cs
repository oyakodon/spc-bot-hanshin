using System;

namespace spc_bot_hanshin
{
    class Program
    {
        static void Main(string[] args)
        {
            new SlackBot().Run();
            while (true) Console.ReadKey(true);
        }
    }
}
