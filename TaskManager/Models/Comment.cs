using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models;

public class Comment
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TaskItem Task { get; set; } = null!;
}
