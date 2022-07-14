using System.Net.Http.Headers;
using Microsoft.Identity.Client;

var app = PublicClientApplicationBuilder
    .Create("79221685-706a-42de-bb1a-b6c095e034a4")
    .WithAuthority(AzureCloudInstance.AzurePublic, "common")
    .WithRedirectUri("http://localhost");

    var token = await app.Build()
    .AcquireTokenInteractive(new string[]{"api://79221685-706a-42de-bb1a-b6c095e034a4/Data.Read"})
    .ExecuteAsync();
    System.Console.WriteLine(token.AccessToken);

    var client = new HttpClient();
    client.BaseAddress = new Uri("https://localhost:7090/");

    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token.AccessToken);

    string data = client.GetStringAsync("weatherforecast").Result;
    System.Console.WriteLine(data);

    Console.ReadLine();