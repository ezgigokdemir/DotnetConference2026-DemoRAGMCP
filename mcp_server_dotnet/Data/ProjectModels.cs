using System.Text.Json.Serialization;

public record SprintTask(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("assignee")] string Assignee,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("story_points")] int StoryPoints
);

public record TeamMember(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("skills")] List<string> Skills,
    [property: JsonPropertyName("current_sprint_points")] int CurrentSprintPoints
);

public record VelocityEntry(
    [property: JsonPropertyName("sprint")] string Sprint,
    [property: JsonPropertyName("committed")] int Committed,
    [property: JsonPropertyName("completed")] int? Completed
);

public record BacklogItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("story_points")] int StoryPoints,
    [property: JsonPropertyName("category")] string Category
);

public record Sprint(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("start_date")] string StartDate,
    [property: JsonPropertyName("end_date")] string EndDate,
    [property: JsonPropertyName("goal")] string Goal,
    [property: JsonPropertyName("tasks")] List<SprintTask> Tasks
);

public record ProjectData(
    [property: JsonPropertyName("sprints")] List<Sprint> Sprints,
    [property: JsonPropertyName("velocity_history")] List<VelocityEntry> VelocityHistory,
    [property: JsonPropertyName("backlog")] List<BacklogItem> Backlog,
    [property: JsonPropertyName("team")] List<TeamMember> Team
);