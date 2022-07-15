using Azure.Storage.Queues;
using System;
using System.Threading.Tasks;

namespace StorageQueueWriter
{
    class Program
    {
        static string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=psstoor;AccountKey=JoZ8fIrOonCWG4ja5Yigzs0tnNNn1cXydeiBKJtLt6korQMP3+ZgsRXbmm8iIt77w5VOLGsIZovz+ASt/BVBHA==;EndpointSuffix=core.windows.net";
        static string QueueName = "lidl";
        static async Task Main(string[] args)
        {
            await WriteToQueueAsync();
            Console.WriteLine("Press Enter to Quit");
            Console.ReadLine();
        }

        private static async Task WriteToQueueAsync()
        {
            var client = new QueueClient(ConnectionString, QueueName);
            for (int i = 0; i < 100; i++)
            {
                await client.SendMessageAsync($"Hello Number {i}");
            }
            
        }

    }
}
