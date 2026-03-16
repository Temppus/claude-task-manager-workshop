using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.DTOs;
using TaskManager.Models;

namespace TaskManager.Controllers;

[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db) => _db = db;

    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
    {
        if (request.DueDate.HasValue && request.DueDate.Value.ToUniversalTime() <= DateTime.UtcNow)
            return BadRequest(new ErrorResponse { Error = "Due date must be in the future." });

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Status = Models.TaskStatus.Todo,
            DueDate = request.DueDate?.ToUniversalTime(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, TaskResponse.FromEntity(task));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null)
    {
        var query = _db.Tasks.AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            var parsedStatus = ParseStatus(status);
            if (parsedStatus == null)
                return BadRequest(new ErrorResponse { Error = $"Invalid status: {status}. Valid values: todo, in_progress, done." });
            query = query.Where(t => t.Status == parsedStatus.Value);
        }

        if (!string.IsNullOrEmpty(priority))
        {
            if (!Enum.TryParse<TaskPriority>(priority, ignoreCase: true, out var parsedPriority))
                return BadRequest(new ErrorResponse { Error = $"Invalid priority: {priority}. Valid values: low, medium, high." });
            query = query.Where(t => t.Priority == parsedPriority);
        }

        var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Ok(tasks.Select(TaskResponse.FromEntity));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null)
            return NotFound(new ErrorResponse { Error = "Task not found." });

        return Ok(TaskResponse.FromEntity(task));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null)
            return NotFound(new ErrorResponse { Error = "Task not found." });

        if (request.Title != null)
        {
            if (request.Title.Length == 0)
                return BadRequest(new ErrorResponse { Error = "Title cannot be empty." });
            task.Title = request.Title;
        }

        if (request.Description != null)
            task.Description = request.Description;

        if (request.Priority.HasValue)
            task.Priority = request.Priority.Value;

        if (request.DueDate.HasValue)
            task.DueDate = request.DueDate.Value.ToUniversalTime();

        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(TaskResponse.FromEntity(task));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null)
            return NotFound(new ErrorResponse { Error = "Task not found." });

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null)
            return NotFound(new ErrorResponse { Error = "Task not found." });

        var currentStatus = task.Status;
        var newStatus = request.Status;

        bool validTransition = (currentStatus, newStatus) switch
        {
            (Models.TaskStatus.Todo, Models.TaskStatus.InProgress) => true,
            (Models.TaskStatus.InProgress, Models.TaskStatus.Done) => true,
            _ => false
        };

        if (!validTransition)
        {
            var currentStr = StatusToString(currentStatus);
            var newStr = StatusToString(newStatus);
            return BadRequest(new ErrorResponse
            {
                Error = $"Invalid status transition from '{currentStr}' to '{newStr}'. Allowed transitions: todo -> in_progress -> done."
            });
        }

        task.Status = newStatus;
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(TaskResponse.FromEntity(task));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(List<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new ErrorResponse { Error = "Search query 'q' is required." });

        var lowerQ = q.ToLowerInvariant();
        var tasks = await _db.Tasks
            .Where(t => t.Title.ToLower().Contains(lowerQ) ||
                        (t.Description != null && t.Description.ToLower().Contains(lowerQ)))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tasks.Select(TaskResponse.FromEntity));
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStats), StatusCodes.Status200OK)]
    public async Task<IActionResult> Stats()
    {
        var tasks = await _db.Tasks.ToListAsync();

        var stats = new DashboardStats
        {
            TotalTasks = tasks.Count,
            ByStatus = new Dictionary<string, int>
            {
                ["todo"] = tasks.Count(t => t.Status == Models.TaskStatus.Todo),
                ["in_progress"] = tasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                ["done"] = tasks.Count(t => t.Status == Models.TaskStatus.Done)
            },
            ByPriority = new Dictionary<string, int>
            {
                ["low"] = tasks.Count(t => t.Priority == TaskPriority.Low),
                ["medium"] = tasks.Count(t => t.Priority == TaskPriority.Medium),
                ["high"] = tasks.Count(t => t.Priority == TaskPriority.High)
            },
            OverdueTasks = tasks.Count(t =>
                t.Status != Models.TaskStatus.Done &&
                t.DueDate.HasValue &&
                t.DueDate.Value < DateTime.UtcNow)
        };

        return Ok(stats);
    }

    private static Models.TaskStatus? ParseStatus(string status) => status.ToLowerInvariant() switch
    {
        "todo" => Models.TaskStatus.Todo,
        "in_progress" => Models.TaskStatus.InProgress,
        "done" => Models.TaskStatus.Done,
        _ => null
    };

    private static string StatusToString(Models.TaskStatus status) => status switch
    {
        Models.TaskStatus.Todo => "todo",
        Models.TaskStatus.InProgress => "in_progress",
        Models.TaskStatus.Done => "done",
        _ => status.ToString()
    };
}
