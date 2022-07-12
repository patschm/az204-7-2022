using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Durability
{
    public static class Orchestrator
    {
        // Triggers the Orchestrator
        [FunctionName("Orchestrator_HttpStart")]
        public static async Task<IActionResult> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{scenario}")] HttpRequestMessage req,
            string scenario,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Starts the Orchestrator with unique identifier that will be used to check states and such
            string instanceId = "";
            switch(scenario.ToLower())
            {
                case "a":
                    {
                        instanceId = await starter.StartNewAsync("FunctionChaining_Orchestrator", "unique_identifier");
                        break;
                    }
                case "b":
                    {
                        instanceId = await starter.StartNewAsync("Fan_Orchestrator", "unique_identifier");
                        break;
                    }
                case "c":
                    {
                        instanceId = await starter.StartNewAsync("Polling_Orchestrator", "unique_identifier");
                        break;
                    }
                default:
                    {
                        instanceId = await starter.StartNewAsync("Pipe_Orchestrator", "unique_identifier");
                        break;
                    }
            }
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            var bld = new StringBuilder();
            var info =  starter.CreateCheckStatusResponse(req, instanceId);
            dynamic data = JsonConvert.DeserializeObject<dynamic>(await info.Content.ReadAsStringAsync());
            //string statusUrl = data.statusQueryGetUri.ToString();
            //string eventUrl = data.sendEventPostUri.ToString();
            //string cancelUrl = data.terminatePostUri.ToString();
            bld.Append("<html><head></head><body><table>");
            bld.Append($"<h1>Info for instance {instanceId}</h1>");
            bld.Append($"<tr><td><h2>statusQueryGetUri: </h2></td><td><h2><a href='{data.statusQueryGetUri}'>{data.statusQueryGetUri}</></h2></td></tr>");
            bld.Append($"<tr><td><h2>sendEventPostUri: </h2></td><td><h2><a href='{data.sendEventPostUri}'>{data.sendEventPostUri}</a></h2></td></tr>");
            bld.Append($"<tr><td><h2>terminatePostUri: </h2></td><td><h2><a href='{data.terminatePostUri}'>{data.terminatePostUri}</a></h2></td></tr>");
            bld.Append("</table></body></html>");
            return new ContentResult { ContentType = "text/html", Content = bld.ToString() };
        }

        [FunctionName("FunctionChaining_Orchestrator")]
        public static async Task<string> RunOrchestratorChain(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var res = await context.CallActivityAsync<string>("ActivitySay", "Hello ");
            log.LogError($"Chaining Result: [{res}]");
            res = await context.CallActivityAsync<string>("ActivitySay", "Durable ");
            log.LogError($"Chaining Result: [{res}]");
            res = await context.CallActivityAsync<string>("ActivitySay", "Functions ");
            log.LogError($"Chaining Result: [{res}]");

            return "OK";
        }
        [FunctionName("Fan_Orchestrator")]
        public static async Task<string> RunOrchestratorFan(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var outputs = new List<Task<string>>();
            outputs.Add(context.CallActivityAsync<string>("ActivitySay", "Hello "));
            outputs.Add(context.CallActivityAsync<string>("ActivitySay", "Durable "));
            outputs.Add(context.CallActivityAsync<string>("ActivitySay", "Functions "));

            await Task.WhenAll(outputs);
            var result = "";
            outputs.ForEach(item => result += $"[{item.Result}], " );
            log.LogError($"Fan Result: {result}");
            return result;
        }
        [FunctionName("Polling_Orchestrator")]
        public static async Task<string> RunOrchestratorPolling(
           [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation($"Use StatusQueryGetUri to check the status");
            string result = "";
            for (int i = 0; i < 30; i++)
            {
                result += await context.CallActivityAsync<string>("ActivitySay", $"Hello {i}");
                result += "\r\n";
            }
            await context.WaitForExternalEvent("bla");
            log.LogInformation($"Polling Result: {result}");
            return result;
        }
        [FunctionName("Monitor_Orchestrator")]
        public static async Task<string> RunOrchestratorMonitor(
          [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            for (int i = 0; i < 10; i++)
            {
                var status = await context.CallActivityAsync<string>("SomeJob", $"Hello Monitor {i}");
                if (status == "Completed")
                {

                }
            }
            log.LogError($"Monitor Result");
            return "OK";
        }

        [FunctionName("ActivitySay")]
        public static async Task<string> SayActivityAsync([ActivityTrigger] string name, ILogger log)
        {
            await Task.Delay(1000);
            log.LogError($"Activity Says: {name}.");
            return $"{name}!";
        }
        [FunctionName("SomeJob")]
        public static async Task SomeJobActivityAsync([ActivityTrigger] string name, ILogger log)
        {
            await Task.Delay(1000);
            log.LogError($"Job Says: {name}.");
        }
    }
}