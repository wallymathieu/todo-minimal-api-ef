
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Testcontainers.MsSql;
using Xunit;
namespace Todo.Api.Tests;
public class DbFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer;

    public DbFixture()
    {
        _dbContainer = new MsSqlBuilder()
           .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
           .WithPassword("Strong_password_123!")
           .WithHostname(Guid.NewGuid().ToString("N"))
           .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        var opts = new DbContextOptionsBuilder()
            .UseSqlServer(_dbContainer.GetConnectionString())
            .Options;
        using var db = new TodoDb(opts);
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    public string GetConnectionString() => _dbContainer.GetConnectionString();
}
public class ApiFixture : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _testServer;
    private readonly DbFixture _dbFixture;
    private readonly WebApplicationFactory<Program> factory;

    public ApiFixture()
    {
        _dbFixture = new DbFixture();
        factory = new WebApplicationFactory<Program>();
        _testServer = factory.WithWebHostBuilder(c => c.UseConfiguration(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                   {"AZURE_SQL_CONNECTION_STRING_KEY", "CONNECTION_STRING"},
                   {"CONNECTION_STRING", _dbFixture.GetConnectionString()}
                }).Build()));
    }

    public async Task InitializeAsync()
    {
        await _dbFixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        _testServer.Dispose();
        await _dbFixture.DisposeAsync();
    }

    public TestServer Server => _testServer.Server;
}

public class ContractTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{

    [Fact]
    public async Task Can_save_and_get_todo()
    {
        using var client = fixture.Server.CreateClient();
        var createdTodo = await client.PostAsync("/lists",
            new StringContent(@"{
                        ""Name"": ""Test""
                    }", Encoding.UTF8, "application/json"));
        createdTodo.EnsureSuccessStatusCode();
        var obj = JObject.Parse(await createdTodo.Content.ReadAsStringAsync());
        var id = obj["id"].Value<string>();
        Assert.NotNull(id);
        var todoResponse = await client.GetAsync("/lists/" + id);
        todoResponse.EnsureSuccessStatusCode();
        var todoId = JObject.Parse(await todoResponse.Content.ReadAsStringAsync())["id"].Value<string>();
        Assert.Equal(id, todoId);
    }
}