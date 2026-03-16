using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TaskManager.DTOs;

namespace TaskManager.Tests;

public class CommentsApiTests : IClassFixture<TaskManagerApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public CommentsApiTests(TaskManagerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Create Comment ───────────────────────────────────────

    [Fact]
    public async Task Test_CreateComment_ReturnsCreatedWithCorrectFields()
    {
        var task = await CreateTask("Comment target");

        var response = await _client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments",
            new { author = "Alice", body = "Great progress!" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var comment = await response.Content.ReadFromJsonAsync<CommentResponse>(JsonOptions);
        comment.Should().NotBeNull();
        comment!.TaskId.Should().Be(task.Id);
        comment.Author.Should().Be("Alice");
        comment.Body.Should().Be("Great progress!");
        comment.Id.Should().NotBeEmpty();
        comment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Test_CreateComment_MissingAuthor_ReturnsBadRequest()
    {
        var task = await CreateTask("No author test");

        var response = await _client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments",
            new { body = "Some body" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Error.Should().Contain("Author");
    }

    [Fact]
    public async Task Test_CreateComment_MissingBody_ReturnsBadRequest()
    {
        var task = await CreateTask("No body test");

        var response = await _client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments",
            new { author = "Alice" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Error.Should().Contain("Body");
    }

    [Fact]
    public async Task Test_CreateComment_EmptyBodyAfterTrim_ReturnsBadRequest()
    {
        var task = await CreateTask("Empty body test");

        var response = await _client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments",
            new { author = "Alice", body = "   " }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Error.Should().Contain("Body");
    }

    [Fact]
    public async Task Test_CreateComment_WhitespaceOnlyAuthor_ReturnsBadRequest()
    {
        var task = await CreateTask("Whitespace author test");

        var response = await _client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments",
            new { author = "   ", body = "Some body" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Error.Should().Contain("Author");
    }

    [Fact]
    public async Task Test_CreateComment_BodyExceeds1000Chars_ReturnsBadRequest()
    {
        var task = await CreateTask("Long body test");

        var response = await _client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments",
            new { author = "Alice", body = new string('x', 1001) }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Error.Should().Contain("1000");
    }

    [Fact]
    public async Task Test_CreateComment_AuthorExceeds100Chars_ReturnsBadRequest()
    {
        var task = await CreateTask("Long author test");

        var response = await _client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments",
            new { author = new string('a', 101), body = "Some body" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Error.Should().Contain("100");
    }

    [Fact]
    public async Task Test_CreateComment_MultilineBody_PreservesNewlines()
    {
        var task = await CreateTask("Multiline test");
        var multilineBody = "Line 1\nLine 2\nLine 3";

        var response = await _client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments",
            new { author = "Alice", body = multilineBody }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var comment = await response.Content.ReadFromJsonAsync<CommentResponse>(JsonOptions);
        comment!.Body.Should().Be(multilineBody);
    }

    [Fact]
    public async Task Test_CreateComment_TaskNotFound_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync($"/api/tasks/{Guid.NewGuid()}/comments",
            new { author = "Alice", body = "Orphan comment" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── List Comments ────────────────────────────────────────

    [Fact]
    public async Task Test_ListComments_ReturnsOrderedByCreatedAtAsc()
    {
        var task = await CreateTask("List order test");

        await _client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments",
            new { author = "Alice", body = "First" }, JsonOptions);
        await _client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments",
            new { author = "Bob", body = "Second" }, JsonOptions);
        await _client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments",
            new { author = "Charlie", body = "Third" }, JsonOptions);

        var response = await _client.GetAsync($"/api/tasks/{task.Id}/comments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var comments = await response.Content.ReadFromJsonAsync<List<CommentResponse>>(JsonOptions);
        comments!.Should().HaveCountGreaterThanOrEqualTo(3);
        comments.Should().BeInAscendingOrder(c => c.CreatedAt);
        comments[0].Body.Should().Be("First");
    }

    [Fact]
    public async Task Test_ListComments_EmptyList_ForTaskWithNoComments()
    {
        var task = await CreateTask("No comments task");

        var response = await _client.GetAsync($"/api/tasks/{task.Id}/comments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var comments = await response.Content.ReadFromJsonAsync<List<CommentResponse>>(JsonOptions);
        comments!.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_ListComments_TaskNotFound_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/tasks/{Guid.NewGuid()}/comments");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Cascade Delete ───────────────────────────────────────

    [Fact]
    public async Task Test_DeleteTask_CascadesDeleteToComments()
    {
        var task = await CreateTask("Cascade delete test");
        await _client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments",
            new { author = "Alice", body = "Will be deleted" }, JsonOptions);

        var deleteResponse = await _client.DeleteAsync($"/api/tasks/{task.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await _client.GetAsync($"/api/tasks/{task.Id}/comments");
        listResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Isolation ────────────────────────────────────────────

    [Fact]
    public async Task Test_Comments_AreIsolatedPerTask()
    {
        var taskA = await CreateTask("Task A");
        var taskB = await CreateTask("Task B");

        await _client.PostAsJsonAsync($"/api/tasks/{taskA.Id}/comments",
            new { author = "Alice", body = "Comment on A" }, JsonOptions);
        await _client.PostAsJsonAsync($"/api/tasks/{taskB.Id}/comments",
            new { author = "Bob", body = "Comment on B" }, JsonOptions);

        var responseA = await _client.GetAsync($"/api/tasks/{taskA.Id}/comments");
        var commentsA = await responseA.Content.ReadFromJsonAsync<List<CommentResponse>>(JsonOptions);
        commentsA!.Should().AllSatisfy(c => c.TaskId.Should().Be(taskA.Id));
        commentsA.Should().Contain(c => c.Body == "Comment on A");
        commentsA.Should().NotContain(c => c.Body == "Comment on B");
    }

    // ── Helpers ──────────────────────────────────────────────

    private async Task<TaskResponse> CreateTask(string title)
    {
        var request = new { title, priority = "medium" };
        var response = await _client.PostAsJsonAsync("/api/tasks", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions))!;
    }
}
