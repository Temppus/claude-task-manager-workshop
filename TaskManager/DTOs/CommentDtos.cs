using TaskManager.Models;

namespace TaskManager.DTOs;

public class CreateCommentRequest
{
    public string? Author { get; set; }
    public string? Body { get; set; }
}

public class CommentResponse
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static CommentResponse FromEntity(Comment comment) => new()
    {
        Id = comment.Id,
        TaskId = comment.TaskId,
        Author = comment.Author,
        Body = comment.Body,
        CreatedAt = comment.CreatedAt
    };
}
