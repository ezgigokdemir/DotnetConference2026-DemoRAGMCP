using System.ComponentModel;
using ModelContextProtocol.Server;
using PmMcpServer.Services;

namespace PmMcpServer.Tools;

[McpServerToolType]
public class ProjectTools
{
    private readonly ProjectService _projectService;

    public ProjectTools(ProjectService projectService)
    {
        _projectService = projectService;
    }

    [McpServerTool, Description("Returns the status of a sprint including tasks, progress and goal.")]
    public object GetSprintStatus(
        [Description("Sprint ID (e.g. 'SP-1'), sprint name (e.g. 'Sprint 11'), or 'active' for current sprint.")]
        string sprintId = "active")
    {
        try
        {
            return _projectService.GetSprintStatus(sprintId);
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message, stackTrace = ex.StackTrace };
        }
    }

    [McpServerTool, Description("Returns the team's velocity history and average story points per sprint.")]
    public object GetTeamVelocity(
        [Description("Number of past sprints to include (default: 4).")]
        int lastNSprints = 4)
    {
        return _projectService.GetTeamVelocity(lastNSprints);
    }

    [McpServerTool, Description("Returns prioritized items from the product backlog.")]
    public object GetBacklogItems(
        [Description("Filter by priority: 'high', 'medium', 'low', or 'all'.")]
        string priority = "all",
        [Description("Maximum number of items to return (default: 5).")]
        int limit = 5)
    {
        return _projectService.GetBacklogItems(priority, limit);
    }

    [McpServerTool, Description("Returns team members with their roles, skills and current sprint workload.")]
    public object GetTeamMembers(
        [Description("Filter by role keyword (e.g. 'developer', 'scrum master') or 'all'.")]
        string role = "all")
    {
        return _projectService.GetTeamMembers(role);
    }

    [McpServerTool, Description("Returns all tasks assigned to a specific team member across sprints.")]
    public object GetTasksByAssignee(
        [Description("Full name or first name of the team member (e.g. 'Ahmet').")]
        string assignee)
    {
        return _projectService.GetTasksByAssignee(assignee);
    }
}