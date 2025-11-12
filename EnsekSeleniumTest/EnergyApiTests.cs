using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RestSharp;

namespace EnsekSeleniumTest
{
    public class EnergyApiTests
    {
        private const string LoginUrl = "https://qacandidatetest.ensek.io/ENSEK/login";
        private const string EnergyUrl = "https://qacandidatetest.ensek.io/ENSEK/energy";
        private const string BuyUrl = "https://qacandidatetest.ensek.io/ENSEK/buy";
        private const string ResetUrl = "https://qacandidatetest.ensek.io/ENSEK/reset";
        private const string OrdersUrl = "https://qacandidatetest.ensek.io/ENSEK/orders";

        private const int DefaultQuantity = 10;
        private static readonly List<string> OrderIds = new();
        private static string? FirstOrderTime;
        private readonly RestClient client;
        private string? accessToken;

        public EnergyApiTests()
        {
            client = new RestClient();
        }

        private string Authenticate()
        {
            if (!string.IsNullOrEmpty(accessToken)) return accessToken;

            var request = new RestRequest(LoginUrl, Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddStringBody("{\"username\":\"test\",\"password\":\"testing\"}", DataFormat.Json);

            var response = client.Execute(request);
            Assert.Equal(200, (int)response.StatusCode);

            using var json = JsonDocument.Parse(response.Content!);
            accessToken = json.RootElement.GetProperty("access_token").GetString();
            return accessToken!;
        }

        private RestRequest CreateRequest(string url, Method method, bool auth = false)
        {
            var request = new RestRequest(url, method);
            if (auth)
                request.AddHeader("Authorization", $"Bearer {Authenticate()}");
            return request;
        }

        private string BuyEnergy(int energyId, int quantity)
        {
            var request = CreateRequest(BuyUrl, Method.Post, auth: true);
            request.AddHeader("Content-Type", "application/json");
            request.AddStringBody($"{{\"energy_id\":{energyId},\"quantity\":{quantity}}}", DataFormat.Json);

            var response = client.Execute(request);
            Assert.Equal(200, (int)response.StatusCode);

            using var json = JsonDocument.Parse(response.Content!);
            return json.RootElement.GetProperty("id").GetString()!;
        }

        private List<int> GetEnergyIds()
        {
            var request = CreateRequest(EnergyUrl, Method.Get);
            var response = client.Execute(request);
            Assert.Equal(200, (int)response.StatusCode);

            using var json = JsonDocument.Parse(response.Content!);
            var ids = new List<int>();
            foreach (var prop in json.RootElement.EnumerateObject())
            {
                ids.Add(prop.Value.GetProperty("energy_id").GetInt32());
            }
            return ids;
        }

        private void VerifyOrderDetails(string orderId, int quantity)
        {
            var request = CreateRequest($"{OrdersUrl}/{orderId}", Method.Get, auth: true);
            var response = client.Execute(request);
            Assert.Equal(200, (int)response.StatusCode);

            using var json = JsonDocument.Parse(response.Content!);
            Assert.Equal(orderId, json.RootElement.GetProperty("id").GetString());
            Assert.Equal(quantity, json.RootElement.GetProperty("quantity").GetInt32());
        }

        [Fact]
        public void TestUnauthorizedLogin()
        {
            var request = new RestRequest(LoginUrl, Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddStringBody("{\"username\":\"wrongUser\",\"password\":\"wrongPassword\"}", DataFormat.Json);

            var response = client.Execute(request);
            Assert.Equal(401, (int)response.StatusCode);
        }

        [Fact]
        public void TestResetDataAuthorized()
        {
            var request = CreateRequest(ResetUrl, Method.Post, auth: true);
            var response = client.Execute(request);
            Assert.Equal(200, (int)response.StatusCode);
        }

        [Fact]
        public void TestBuyEnergyAndStoreOrders()
        {
            foreach (var id in GetEnergyIds())
            {
                var orderId = BuyEnergy(id, DefaultQuantity);
                OrderIds.Add(orderId);
            }
        }

        [Fact]
        public void TestGetOrdersAndVerifyDetails()
        {
            if (!OrderIds.Any())
                TestBuyEnergyAndStoreOrders();

            var request = CreateRequest(OrdersUrl, Method.Get, auth: true);
            var response = client.Execute(request);
            Assert.Equal(200, (int)response.StatusCode);

            using var json = JsonDocument.Parse(response.Content!);
            var orders = json.RootElement.EnumerateArray().ToList();
            if (orders.Count > 0)
                FirstOrderTime = orders[0].GetProperty("time").GetString();

            foreach (var orderId in OrderIds)
                VerifyOrderDetails(orderId, DefaultQuantity);
        }

        [Fact]
        public void TestCountOrdersBeforeNow()
        {
            if (FirstOrderTime == null)
                TestGetOrdersAndVerifyDetails();

            var request = CreateRequest(OrdersUrl, Method.Get, auth: true);
            var response = client.Execute(request);
            Assert.Equal(200, (int)response.StatusCode);

            using var json = JsonDocument.Parse(response.Content!);
            var times = json.RootElement.EnumerateArray()
                .Select(x => x.TryGetProperty("time", out var t) ? t.GetString()! : string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(t => DateTime.Parse(t))
                .ToList();

            var count = times.Count(d => d < DateTime.UtcNow);
            Console.WriteLine($"Orders before current time: {count}");
        }

        [Fact]
        public void TestBadRequestOnBuy()
        {
            var request = CreateRequest(BuyUrl, Method.Post, auth: true);
            request.AddHeader("Content-Type", "application/json");
            request.AddStringBody("{\"energy_id\":1,\"quantity\":-5}", DataFormat.Json);

            var response = client.Execute(request);
            Assert.Equal(400, (int)response.StatusCode);
        }
    }
}
