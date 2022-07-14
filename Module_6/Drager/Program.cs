using System.Net.Http.Headers;
using Microsoft.Identity.Client;

//ConfidentialClientApplicationBuilder.Create("").WithClientSecret().Build();
var app = PublicClientApplicationBuilder
    .Create("d5298fb9-031c-4625-b4c4-952dabcfe723")
    .WithAuthority(AzureCloudInstance.AzurePublic, "common")
    .WithRedirectUri("http://localhost");

    var token = await app.Build()
   // .AcquireTokenByUsernamePassword
    .AcquireTokenInteractive(new string[]{ "api://28ecd1c7-059d-40a4-86b9-2f04ab4f9178/Access" })
    .ExecuteAsync();
    System.Console.WriteLine(token.AccessToken);

    var client = new HttpClient();
    client.BaseAddress = new Uri("https://localhost:7175/");

    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token.AccessToken);

    string data = await client.GetStringAsync("weatherforecast");
    System.Console.WriteLine(data);

    Console.ReadLine();