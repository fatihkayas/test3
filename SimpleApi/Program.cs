var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseHttpsRedirection();

// In-memory data (şimdilik veritabanı yok)
var todos = new List<TodoItem>();
var nextId = 1;

app.MapGet("/", () => "Hello 👋 Simple .NET Web API çalışıyor!");
app.MapGet("/health", () => Results.Ok(new { status = "OK", time = DateTimeOffset.Now }));

// Tüm todoları getir
app.MapGet("/todos", () => Results.Ok(todos));

// Id ile todo getir
app.MapGet("/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    return todo is null ? Results.NotFound() : Results.Ok(todo);
});

// Todo ekle
app.MapPost("/todos", (CreateTodoRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Title))
        return Results.BadRequest(new { error = "Title boş olamaz." });

    var item = new TodoItem(nextId++, req.Title.Trim(), false);
    todos.Add(item);

    return Results.Created($"/todos/{item.Id}", item);
});

// Todo tamamlandı/ tamamlanmadı yap
app.MapPut("/todos/{id:int}/done", (int id, UpdateDoneRequest req) =>
{
    var index = todos.FindIndex(t => t.Id == id);
    if (index == -1) return Results.NotFound();

    var old = todos[index];
    var updated = old with { IsDone = req.IsDone };
    todos[index] = updated;

    return Results.Ok(updated);
});

// Todo sil
app.MapDelete("/todos/{id:int}", (int id) =>
{
    var removed = todos.RemoveAll(t => t.Id == id);
    return removed == 0 ? Results.NotFound() : Results.NoContent();
});

app.Run();

record TodoItem(int Id, string Title, bool IsDone);
record CreateTodoRequest(string Title);
record UpdateDoneRequest(bool IsDone);

public partial class Program { }
