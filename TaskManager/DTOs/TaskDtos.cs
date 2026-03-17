using System.ComponentModel.DataAnnotations;
using TaskManager.Models;

namespace TaskManager.DTOs;

public class CreateTaskRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(255, ErrorMessage = "Title must be at most 255 characters.")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }
}

public class UpdateTaskRequest
{
    [MaxLength(255, ErrorMessage = "Title must be at most 255 characters.")]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public TaskPriority? Priority { get; set; }

    public DateTime? DueDate { get; set; }
}

public class UpdateStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    public Models.TaskStatus Status { get; set; }
}

public class TaskResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static TaskResponse FromEntity(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Priority = task.Priority.ToString().ToLowerInvariant(),
        Status = task.Status switch
        {
            Models.TaskStatus.Todo => "todo",
            Models.TaskStatus.InProgress => "in_progress",
            Models.TaskStatus.Done => "done",
            _ => task.Status.ToString().ToLowerInvariant()
        },
        DueDate = task.DueDate,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}

public class DashboardStats
{
    public int TotalTasks { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> ByPriority { get; set; } = new();
    public int OverdueTasks { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}
