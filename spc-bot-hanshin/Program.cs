using System;
using System.Threading.Tasks;

namespace spc_bot_hanshin
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("###########################");
            Console.WriteLine("##### spc-bot-hanshin #####");
            Console.WriteLine("###########################");

            try
            {
                var t = Task.Run(() => new SlackBot().Run());
                t.Wait();
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e.InnerException);
            }
        }
    }
}
