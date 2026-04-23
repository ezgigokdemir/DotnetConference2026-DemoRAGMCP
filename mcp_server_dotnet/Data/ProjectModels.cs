namespace PmMcpServer.Data;

public record SprintTask(
    string Id,
    string Title,
    string Assignee,
    string Status,
    int StoryPoints
);

public record Sprint(
    string Id,
    string Name,
    string Status,
    string StartDate,
    string EndDate,
    string Goal,
    List<SprintTask> Tasks
);

public record VelocityEntry(
    string Sprint,
    int Committed,
    int? Completed
);

public record BacklogItem(
    string Id,
    string Title,
    string Priority,
    int StoryPoints,
    string Category
);

public record TeamMember(
    string Name,
    string Role,
    List<string> Skills,
    int CurrentSprintPoints
);

public record ProjectData(
    List<Sprint> Sprints,
    List<VelocityEntry> VelocityHistory,
    List<BacklogItem> Backlog,
    List<TeamMember> Team
);