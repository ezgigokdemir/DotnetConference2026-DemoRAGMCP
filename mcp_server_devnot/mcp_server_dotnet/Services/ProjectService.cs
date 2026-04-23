using System.Diagnostics;
using System.Text.Json;
using PmMcpServer.Data;

namespace PmMcpServer.Services;

public class ProjectService
{
    private readonly ProjectData _data;
    private static readonly string LogFile = Path.Combine(
        Directory.GetCurrentDirectory(), "benchmark_results.txt");

    public ProjectService()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory, "Data", "project_data.json");
    
        if (!File.Exists(path))
        {
            // fallback: proje dizini
            path = Path.Combine(
                Directory.GetCurrentDirectory(), "Data", "project_data.json");
        }
    
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        _data = JsonSerializer.Deserialize<ProjectData>(json, options)!;
    }

    private void Log(string toolName, double executionMs)
    {
        var separator = new string('=', 60);
        File.AppendAllText(LogFile,
            $"\n{separator}\n" +
            $"[{DateTime.Now:HH:mm:ss}] {toolName}\n" +
            $"  Execution : {executionMs} ms\n");
    }

    public object GetSprintStatus(string sprintId = "active")
    {
        var sw = Stopwatch.StartNew();
        Sprint? sprint = sprintId == "active"
            ? _data.Sprints.FirstOrDefault(s => s.Status == "active")
            : _data.Sprints.FirstOrDefault(s =>
                s.Id.Equals(sprintId, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Equals(sprintId, StringComparison.OrdinalIgnoreCase));

        sw.Stop();
        Log("GetSprintStatus", Math.Round(sw.Elapsed.TotalMilliseconds, 2));

        if (sprint is null)
            return new { success = false, message = $"Sprint not found: {sprintId}" };

        var done = sprint.Tasks.Count(t => t.Status == "done");
        var inProgress = sprint.Tasks.Count(t => t.Status == "in_progress");
        var todo = sprint.Tasks.Count(t => t.Status == "todo");

        return new
        {
            success = true,
            execution_ms = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
            sprint = new
            {
                sprint.Id, sprint.Name, sprint.Status,
                sprint.Goal, sprint.StartDate, sprint.EndDate,
                summary = new { total_tasks = sprint.Tasks.Count, done, in_progress = inProgress, todo },
                tasks = sprint.Tasks
            }
        };
    }

    public object GetTeamVelocity(int lastNSprints = 4)
    {
        var sw = Stopwatch.StartNew();
        var history = _data.VelocityHistory
            .Where(v => v.Completed.HasValue)
            .TakeLast(lastNSprints).ToList();

        var avg = history.Count > 0
            ? Math.Round(history.Average(v => v.Completed!.Value), 1) : 0;

        string trend = "stable";
        if (history.Count >= 2)
        {
            var last = history[^1].Completed!.Value;
            var prev = history[^2].Completed!.Value;
            trend = last > prev ? "improving" : last < prev ? "declining" : "stable";
        }

        sw.Stop();
        Log("GetTeamVelocity", Math.Round(sw.Elapsed.TotalMilliseconds, 2));

        return new
        {
            success = true,
            execution_ms = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
            velocity = new { average = avg, history, trend }
        };
    }

    public object GetBacklogItems(string priority = "all", int limit = 5)
    {
        var sw = Stopwatch.StartNew();
        var items = _data.Backlog.AsEnumerable();

        if (priority != "all")
            items = items.Where(i => i.Priority == priority.ToLower());

        var order = new Dictionary<string, int> { ["high"] = 0, ["medium"] = 1, ["low"] = 2 };
        var result = items.OrderBy(i => order.GetValueOrDefault(i.Priority, 99))
                          .Take(limit).ToList();

        sw.Stop();
        Log("GetBacklogItems", Math.Round(sw.Elapsed.TotalMilliseconds, 2));

        return new
        {
            success = true,
            execution_ms = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
            backlog = new
            {
                count = result.Count,
                filter = priority,
                items = result,
                total_points = result.Sum(i => i.StoryPoints)
            }
        };
    }

    public object GetTeamMembers(string role = "all")
    {
        var sw = Stopwatch.StartNew();
        var members = _data.Team.AsEnumerable();

        if (role != "all")
            members = members.Where(m =>
                m.Role.Contains(role, StringComparison.OrdinalIgnoreCase));

        var result = members.ToList();
        sw.Stop();
        Log("GetTeamMembers", Math.Round(sw.Elapsed.TotalMilliseconds, 2));

        return new
        {
            success = true,
            execution_ms = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
            team = new { count = result.Count, members = result }
        };
    }

    public object GetTasksByAssignee(string assignee)
    {
        var sw = Stopwatch.StartNew();
        var tasks = _data.Sprints
            .SelectMany(s => s.Tasks
                .Where(t => t.Assignee.Equals(assignee, StringComparison.OrdinalIgnoreCase))
                .Select(t => new { t.Id, t.Title, t.Assignee, t.Status, t.StoryPoints, Sprint = s.Name }))
            .ToList();

        sw.Stop();
        Log("GetTasksByAssignee", Math.Round(sw.Elapsed.TotalMilliseconds, 2));

        if (tasks.Count == 0)
            return new { success = false, message = $"No tasks found for: {assignee}" };

        return new
        {
            success = true,
            execution_ms = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
            assignee, count = tasks.Count, tasks
        };
    }
}