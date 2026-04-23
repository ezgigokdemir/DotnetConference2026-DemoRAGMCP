import time
from server import mcp
from services.project_service import (
    get_sprint_status,
    get_team_velocity,
    get_backlog_items,
    get_team_members,
    get_tasks_by_assignee_sprint
)


@mcp.tool()
def get_sprint_status_tool(sprint_id: str = "active") -> dict:
    """
    Returns the status of a sprint including tasks, progress and goal.
    Args:
        sprint_id: Sprint ID (e.g. 'SP-1'), sprint name (e.g. 'Sprint 11'), 
                   or 'active' for the current sprint.
    """
    return get_sprint_status(sprint_id)

@mcp.tool()
def get_team_velocity_tool(last_n_sprints: int = 4) -> dict:
    """
    Returns the team's velocity history and average story points per sprint.
    Args:
        last_n_sprints: Number of past sprints to include (default: 4).
    """
    return get_team_velocity(last_n_sprints)


@mcp.tool()
def get_backlog_items_tool(priority: str = "all", limit: int = 5) -> dict:
    """
    Returns prioritized items from the product backlog.
    Args:
        priority: Filter by priority — 'high', 'medium', 'low', or 'all'.
        limit: Maximum number of items to return (default: 5).
    """
    return get_backlog_items(priority, limit)


@mcp.tool()
def get_team_members_tool(role: str = "all") -> dict:
    """
    Returns team members with their roles, skills and current sprint workload.
    Args:
        role: Filter by role keyword (e.g. 'developer', 'scrum master') or 'all'.
    """
    return get_team_members(role)


@mcp.tool()
def get_tasks_by_assignee_sprint_tool(assignee: str) -> dict:
    """
    Returns all tasks assigned to a specific team member across sprints.
    Args:
        assignee: Full name or first name of the team member (e.g. 'Ahmet').
    """
    return get_tasks_by_assignee_sprint(assignee)