using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.DTOs;
using TaskManager.Models;

namespace TaskManager.Controllers;

[ApiController]
[Route("api/tasks/{taskId:guid}/comments")]
[Produces("application/json")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CommentsController(AppDbContext db) => _db = db;

    /// <summary>Add a comment to a task.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid taskId, [FromBody] CreateCommentRequest request)
    {
        var task = await _db.Tasks.FindAsync(taskId);
        if (task == null)
            return NotFound(new ErrorResponse { Error = "Task not found." });

        var author = request.Author?.Trim();
        var body = request.Body?.Trim();

        if (string.IsNullOrEmpty(author))
            return BadRequest(new ErrorResponse { Error = "Author is required." });

        if (author.Length > 100)
            return BadRequest(new ErrorResponse { Error = "Author must be at most 100 characters." });

        if (string.IsNullOrEmpty(body))
            return BadRequest(new ErrorResponse { Error = "Body is required." });

        if (body.Length > 1000)
            return BadRequest(new ErrorResponse { Error = "Body must be at most 1000 characters." });

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            Author = author,
            Body = body,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return Created($"/api/tasks/{taskId}/comments/{comment.Id}", CommentResponse.FromEntity(comment));
    }

    /// <summary>List all comments for a task, ordered oldest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CommentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(Guid taskId)
    {
        var taskExists = await _db.Tasks.AnyAsync(t => t.Id == taskId);
        if (!taskExists)
            return NotFound(new ErrorResponse { Error = "Task not found." });

        var comments = await _db.Comments
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return Ok(comments.Select(CommentResponse.FromEntity));
    }
}
