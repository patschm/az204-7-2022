using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFunctions;

public interface ICounterService
{
    Task<int> GetVal();
    void Increment(int val = 1);
}

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public class CounterService : ICounterService
{
    [JsonProperty("value")]
    private int data = 0;

    public Task<int> GetVal() => Task.FromResult(this.data);

    public void Increment(int val = 1)
    {
        data += val;
    }
}

