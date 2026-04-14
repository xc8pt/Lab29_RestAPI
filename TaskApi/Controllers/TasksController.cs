using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Protocol;
using TaskApi.Models;

namespace TaskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase {
    private static List<TaskItem> _tasks = new() {
        new TaskItem {
            Id = 1,
            Title = "Изучить ASP.NET Core",
            Priority = "High",
            IsCompleted = true
        },
        new TaskItem {
            Id = 2,
            Title = "Сделать лабораторную №28",
            Priority = "High",
            IsCompleted = false
        },
        new TaskItem {
            Id = 3,
            Title = "Написать README",
            Priority = "Normal",
            IsCompleted = false
        },
    };
    private static int _nextId = 4;

    // Метод GET — получить все задачи
    [HttpGet]
    public ActionResult<IEnumerable<TaskItem>> GetAll([FromQuery] bool? completed = null) {
        var result = _tasks.AsEnumerable();

        if (completed.HasValue)
            result = result.Where(t => t.IsCompleted == completed.Value);

        return Ok(result);
    }

    // Метод GET — получить одну задачу
    [HttpGet("{id}")]
    public ActionResult<TaskItem> GetById(int id) {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task is null)
            return NotFound(new { Message = $"Задача с id={id} не найдена" });
        return Ok(task);
    }

    // Метод POST — создать задачу
    [HttpPost]
    public ActionResult<TaskItem> create([FromBody] CreateTaskDto dto) {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { Message = "Поле Title обязательно для заполнения" });
        var newTask = new TaskItem {
            Id = _nextId++,
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            IsCompleted = false,
            CreatedAt = DateTime.Now
        };
        _tasks.Add(newTask);
        return CreatedAtAction(nameof(GetById), new { id = newTask }, newTask);
    }

    // Метод PUT — обновить задачу
    [HttpPut("{id}")]
    public ActionResult<TaskItem> Update(int id, [FromBody] UpdateTaskDto dto) {
        var task = _tasks.FirstOrDefault(t => t.Id == id);

        if (task is null)
            return NotFound(new { Message = $"Задача с id = {id} не найдена" });

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { Message = "Поле Title не может быть пустым" });

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.IsCompleted = false;
        task.Priority = dto.Priority;

        return Ok(task);
    }

    // Метод DELETE — удалить задачу
    [HttpDelete("{id}")]
    public ActionResult Delete(int id) {
        var task = _tasks.FirstOrDefault(t => t.Id == id);

        if (task is null)
            return NotFound(new { Message = $"Задача с id = {id} не найдена" });
        _tasks.Remove(task);

        return NoContent();

    }

    // Дополнительный метод PATCH — отметить выполненной
    [HttpPatch("{id}/complete")]
    public ActionResult<TaskItem> MarkComplete(int id) {
        var task = _tasks.FirstOrDefault(t => t.Id == id);

        if (task is null)
            return NotFound(new { Message = $"Задача с id = {id} не найдена" });

        task.IsCompleted = !task.IsCompleted;

        return Ok(task);
    }

    // Добавляем маршрут поиска

    [HttpGet("search")]
    public ActionResult<IEnumerable<TaskItem>> Search([FromQuery] string query) {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { Message = "Параметр query не может быть пустым" });
        var results = _tasks
            .Where(t => t.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || t.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Ok(results);
    }

    // Добавляем маршрут фильтрации по приоритету
    [HttpGet("priority{level}")]
    public ActionResult<IEnumerable<TaskItem>> GetByPriority(string level) {
        var allowed = new[] { "Low", "Normal", "High" };

        if (!allowed.Contains(level, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { Message = "Допустимые значения: Low, Normal, High" });

        var results = _tasks
            .Where(t => t.Priority.Equals(level, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Ok(results);
    }

    //  Добавляем маршрут статистики

    [HttpGet("stats")]
    public ActionResult GetStats() {
        var total = _tasks.Count;
        var completed = _tasks.Count(t => t.IsCompleted);
        var pending = total - completed;

        var stats = new {
            Total = total,
            Completed = completed,
            Pending = pending,
            CompletionPct = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0,
            ByPriority = new {
                High = _tasks.Count(t => t.Priority == "High"),
                Normal = _tasks.Count(t => t.Priority == "Normal"),
                Low = _tasks.Count(t => t.Priority == "Low"),
            }
        };

        return Ok(stats);
    }

    // Добавляем маршрут сортировки
    [HttpGet("sorted")]
    public ActionResult<IEnumerable<TaskItem>> GetSorted(
        [FromQuery] string by = "id",
        [FromQuery] bool desc = false) {
        IEnumerable<TaskItem> sorted = by.ToLower() switch {
            "title" => _tasks.OrderBy(t => t.Title),
            "priority" => _tasks.OrderBy(t => t.Priority),
            "createdat" => _tasks.OrderBy(t => t.CreatedAt),
            _ => _tasks.OrderBy(t => t.Id),
        };

        if (desc)
            sorted = sorted.Reverse();

        return Ok(sorted);
    }
}