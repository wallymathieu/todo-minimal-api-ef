
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
public class DbFixture:IDisposable
{
    // ugly (should await start container):
    protected Lazy<MsSqlContainer> _container = new(() =>
    {
        var _dbContainer = new MsSqlBuilder()
           .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
           .WithPassword("Strong_password_123!")
           .WithHostname(Guid.NewGuid().ToString("N"))
           .Build();
        _dbContainer.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        var opts = new DbContextOptionsBuilder()
            .UseSqlServer(_dbContainer.GetConnectionString())
            .Options;
        using var db = new TodoDb(opts);
        db.Database.EnsureCreated();
        return _dbContainer;
    });

    public virtual void Dispose()
    {
        _container.Value.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
public class ApiFixture: DbFixture,IDisposable
{
    WebApplicationFactory<Program> Create()
    {
        var factory = new WebApplicationFactory<Program>();
        return factory.WithWebHostBuilder(c => c.UseConfiguration(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                   {"AZURE_SQL_CONNECTION_STRING_KEY", "CONNECTION_STRING"},
                   {"CONNECTION_STRING",_container.Value.GetConnectionString()}
                }).Build()));
    }
    private readonly WebApplicationFactory<Program> _testServer;
    public ApiFixture()
    {
        _testServer = Create();
    }

    public override void Dispose()
    {
        _testServer.Dispose();
        base.Dispose();
    }
    public TestServer Server => _testServer.Server;
}

public class ContractTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;
    public ContractTests(ApiFixture fixture) => this._fixture = fixture;

    [Fact]
    public async Task Can_save_and_get_todo()
    {
        using var client = _fixture.Server.CreateClient();
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