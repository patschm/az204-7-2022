using Gremlin.Net.Process.Traversal;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using Repository.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDemo
{
    static class CoreSqlTest
    {
        public class Product
        {
            public class ProductGroup
            {
                public int ID { get; set; }
                public string Name { get; set; }
            }
            [JsonProperty(PropertyName = "id")]
            public string ID { get; set; }
            [JsonProperty(PropertyName = "pkey")]
            public string ProductID { get; set; }
            public string Name { get; set; }
            public string Brand { get; set; }
            public string BrandWebSite { get; set; }
            public ProductGroup[] ProductGroups { get; set; }
        }

        private static string Host = "https://ps-cosmonaut.documents.azure.com:443/";
        private static string PrimaryKey = "7UACm5pj4ckKXGfwvTFPsdbepJHvb8BkpRSDwz7B2XiCActNYNZ0aop4MdlXVhVT4ilhHOfTOrJcZBvEf6ahTw==";
        private static string Database = "productDB";
        private static string Container = "products";
        public static int Port = 443;
        public static bool EnableSSL = true;

        public static async Task RunCoreSql()
        {
            var client = CreateCoreSqlClient();
            await AddProductGroups(client);
            //await ReadData(client);
            Console.WriteLine("Done");

        }
        private static CosmosClient CreateCoreSqlClient()
        {
            return new CosmosClient(Host, PrimaryKey);
        }
        
        private static async Task ReadData(CosmosClient client)
        {
            var query = "SELECT * FROM p WHERE STARTSWITH(p.Name, 'D')";
            var pContainer = client.GetContainer(Database, Container);
            var qDef = new QueryDefinition(query);
            string continuationToken = null;

            FeedIterator<Product> iterator = pContainer.GetItemQueryIterator<Product>(qDef, continuationToken);
            while(iterator.HasMoreResults)
            {
                FeedResponse<Product> fResponse = await iterator.ReadNextAsync();
                foreach(var item in fResponse)
                {
                    Console.WriteLine($"{item.Name}. Nr of products: {item.ProductGroups.Count()}");
                }
            }
            Console.WriteLine("====================================================");
            query = @"SELECT p.Name AS GroupName, root.Brand, root.Name AS ProductName 
                            FROM root
                            JOIN p IN root.ProductGroups
                            WHERE p.Name = 'Digitale Camera\'s'";
            qDef = new QueryDefinition(query);
            FeedIterator<dynamic> pIterator = pContainer.GetItemQueryIterator<dynamic>(qDef);
            while (pIterator.HasMoreResults)
            {
                FeedResponse<dynamic> pResponse = await pIterator.ReadNextAsync();
                foreach (var item in pResponse)
                {
                    Console.WriteLine($"{item.GroupName}: {item.Brand} {item.ProductName}");
                }
            }
            Console.WriteLine("====================================================");
            var linq = pContainer.GetItemLinqQueryable<Product>();
            var fi = linq.Where(g => g.ID == "1").ToFeedIterator<Product>();
            while (fi.HasMoreResults)
            {
                var fResponse = await fi.ReadNextAsync();
                foreach (var item in fResponse)
                {
                    Console.WriteLine($"({item.ID}) {item.Name}");
                }
            }
        }
        private static async Task AddProductGroups(CosmosClient client)
        {
            var pContainer = client.GetContainer(Database, Container);
            var groups = await CreateGroupsAsync();
            foreach(var group in groups)
            {
                try
                {
                    var response = await pContainer.CreateItemAsync(group, new PartitionKey(group.ProductID));
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

        }
        private static async Task<List<Product>> CreateGroupsAsync()
        {
            var groups = new List<Product>();
            var bRepo = new BrandRepository();
            var pRepo = new ProductRepository();
            var gRepo = new ProductGroupRepository();

            foreach (var p in await pRepo.GetAllAsync(0, 1000))
            {
                var grps = await gRepo.GetProductGroupsAsync(p.ID);
                var npg = new CoreSqlTest.Product
                {
                    ID = p.ID.ToString(),
                    Name = p.Name,
                    ProductID = p.ID.ToString(),
                    Brand = p.Brand?.Name,
                    BrandWebSite = p.Brand?.Website,
                    ProductGroups = grps.Select(p => new CoreSqlTest.Product.ProductGroup
                    {
                        ID = p.ID,
                        Name = p.Name
                    }).ToArray()
                };
                groups.Add(npg);
            }
            return groups;
        }
    }
}
