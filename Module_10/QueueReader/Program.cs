using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.ServiceBus;

namespace QueueReader
{
    class Program
    {
        static string EndPoint = "zeurbus.servicebus.windows.net";
        static (string Name, string KeY) SasKeyReader = ("Lezer", "8ZUfyfvkIAashALQ7IAW2zEnowQ13z+Mz6lG61HdDoY=");
        static string QueueName = "jumbo";

        static async Task Main(string[] args)
        {
            //await ReadQueueAsync();
            await ReadQueueProcessorAsync();
            Console.WriteLine("Press Enter to Quit");
            Console.ReadLine();
        }

        private static async Task ReadQueueAsync()
        {
            var cred = new AzureNamedKeyCredential(SasKeyReader.Name, SasKeyReader.KeY);
            var client = new ServiceBusClient(EndPoint, cred);
            var receiver = client.CreateReceiver(QueueName);
            do
            {
                var msg = await receiver.ReceiveMessageAsync();
                Console.WriteLine($"Lock Duration: {msg.LockedUntil} Lock Token: {msg.LockToken}");
                var data = msg.Body.ToString();
                Console.WriteLine(data);
                await receiver.CompleteMessageAsync(msg);
                //await receiver.AbandonMessageAsync(msg);
                //await receiver.RenewMessageLockAsync(msg);
                await Task.Delay(1000);
            }
            while (true);
        }
        private static async Task ReadQueueProcessorAsync()
        {
            var cred = new AzureNamedKeyCredential(SasKeyReader.Name, SasKeyReader.KeY);
            var client = new ServiceBusClient(EndPoint, cred);
            //ServiceBusProcessorOptions opts = new ServiceBusProcessorOptions { }
            var receiver = client.CreateProcessor(QueueName);
            
            receiver.ProcessMessageAsync += async evtArg => {
                var msg = evtArg.Message;
                Console.WriteLine($"Lock Duration: {msg.LockedUntil} Lock Token: {msg.LockToken}");
                var data = msg.Body.ToString();
                Console.WriteLine($"Van sessie: {msg.SessionId}: {data}");
                await evtArg.CompleteMessageAsync(msg);
                //await Task.Delay(1000);
                //Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
            };

            receiver.ProcessErrorAsync += evtArg => {
                Console.WriteLine("Ooops");
                Console.WriteLine(evtArg.Exception.Message);
                return Task.CompletedTask;
            };

            await receiver.StartProcessingAsync();
            Console.WriteLine("Press Enter to quit processing");
            Console.ReadLine();
            await receiver.StopProcessingAsync();

        }
    }
}
