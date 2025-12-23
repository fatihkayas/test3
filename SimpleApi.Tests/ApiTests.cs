using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Returns_OK()
    {
        var res = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Root_Returns_Hello()
    {
        var text = await _client.GetStringAsync("/");
        Assert.Contains("Hello", text);
    }

    [Fact]
    public async Task Todos_CRUD_Works()
    {
        // Create
        var createRes = await _client.PostAsJsonAsync("/todos", new { title = "Ekmek al" });
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var created = await createRes.Content.ReadFromJsonAsync<TodoDto>();
        Assert.NotNull(created);
        Assert.True(created!.id > 0);
        Assert.Equal("Ekmek al", created.title);
        Assert.False(created.isDone);

        // List
        var list = await _client.GetFromJsonAsync<TodoDto[]>("/todos");
        Assert.NotNull(list);
        Assert.Contains(list!, t => t.id == created.id);

        // Mark done
        var doneRes = await _client.PutAsJsonAsync($"/todos/{created.id}/done", new { isDone = true });
        Assert.Equal(HttpStatusCode.OK, doneRes.StatusCode);

        var updated = await doneRes.Content.ReadFromJsonAsync<TodoDto>();
        Assert.NotNull(updated);
        Assert.True(updated!.isDone);

        // Delete
        var delRes = await _client.DeleteAsync($"/todos/{created.id}");
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);
    }

    private sealed record TodoDto(int id, string title, bool isDone);
}
