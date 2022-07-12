using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace EntityFunctions;

public static class EntityFunctions
{
    [FunctionName("EntityFunctions_HttpStart")]
    public static async Task<HttpResponseMessage> HttpStart(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
          [DurableClient] IDurableOrchestrationClient starter,
          ILogger log)
    {
        string instanceId = await starter.StartNewAsync("EntityFunctions_Class_Based", null);
        log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        instanceId = await starter.StartNewAsync("EntityFunctions_Counter", null);
        log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        return starter.CreateCheckStatusResponse(req, instanceId);
    }

    [FunctionName("EntityFunctions_Class_Based")]
    public static async Task<int> RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        var entityId = new EntityId(nameof(CounterService), "mijn");

        await context.CallEntityAsync(entityId, nameof(CounterService.Increment), 5);
        var result = await context.CallEntityAsync<int>(entityId, nameof(CounterService.GetVal));
        log.LogInformation(result.ToString());
        await context.CallEntityAsync(entityId, nameof(CounterService.Increment), 5);
        result = await context.CallEntityAsync<int>(entityId, nameof(CounterService.GetVal));
        log.LogInformation(result.ToString());
        return result;
    }

    [FunctionName("EntityFunctions_Counter")]
    public static async Task<string> SignallingAsync(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [DurableClient]IDurableEntityClient svc, 
        ILogger log)
    {
        var entityId = new EntityId(nameof(CounterService), "mijn");
        // Signal is fire and forget (one-way)
        // You won't get a response
        await svc.SignalEntityAsync<ICounterService>(entityId, svc => svc.Increment());

        var response = await svc.ReadEntityStateAsync<JObject>(entityId);
        log.LogInformation($"Counter response: { response.EntityState}");
        return $"End Signalling";
    }

    [FunctionName(nameof(CounterService))]
    public static Task Run([EntityTrigger] IDurableEntityContext ctx)
    {
        return ctx.DispatchAsync<CounterService>();
    }
}
