import json
import time
from pathlib import Path
from typing import Any
from datetime import datetime

LOG_FILE = Path(__file__).parent.parent / "benchmark_results.txt"

_data = None

def _load():
    global _data
    if _data is None:
        path = Path(__file__).parent.parent / "data" / "project_data.json"
        _data = json.loads(path.read_text(encoding="utf-8"))
    return _data

def _log(tool_name: str, execution_ms: float):
    with open(LOG_FILE, "a", encoding="utf-8") as f:
        f.write(f"\n{'='*60}\n")
        f.write(f"[{datetime.now().strftime('%H:%M:%S')}] {tool_name}\n")
        f.write(f"  Execution : {execution_ms} ms\n")

def get_sprint_status(sprint_id: str = "active") -> dict[str, Any]:
    t0 = time.perf_counter()
    data = _load()

    if sprint_id == "active":
        sprint = next((s for s in data["sprints"] if s["status"] == "active"), None)
    else:
        sprint = next(
            (s for s in data["sprints"]
             if s["id"].lower() == sprint_id.lower() 
             or s["name"].lower() == sprint_id.lower()),
            None
        )

    if not sprint:
        return {"success": False, "message": f"Sprint not found: {sprint_id}"}

    done = [t for t in sprint["tasks"] if t["status"] == "done"]
    in_progress = [t for t in sprint["tasks"] if t["status"] == "in_progress"]
    todo = [t for t in sprint["tasks"] if t["status"] == "todo"]

    execution_ms = round((time.perf_counter() - t0) * 1000, 2)
    _log("get_sprint_status", execution_ms)

    return {
        "success": True,
        "execution_ms": execution_ms,
        "sprint": {
            "id": sprint["id"],
            "name": sprint["name"],
            "status": sprint["status"],
            "goal": sprint["goal"],
            "start_date": sprint["start_date"],
            "end_date": sprint["end_date"],
            "summary": {
                "total_tasks": len(sprint["tasks"]),
                "done": len(done),
                "in_progress": len(in_progress),
                "todo": len(todo)
            },
            "tasks": sprint["tasks"]
        }
    }

def get_team_velocity(last_n_sprints: int = 4) -> dict[str, Any]:
    t0 = time.perf_counter()
    data = _load()

    history = [v for v in data["velocity_history"] if v["completed"] is not None]
    history = history[-last_n_sprints:]
    avg = round(sum(v["completed"] for v in history) / len(history), 1) if history else 0

    trend = "stable"
    if len(history) >= 2:
        last = history[-1]["completed"]
        prev = history[-2]["completed"]
        trend = "improving" if last > prev else "declining" if last < prev else "stable"

    execution_ms = round((time.perf_counter() - t0) * 1000, 2)
    _log("get_team_velocity", execution_ms)

    return {
        "success": True,
        "execution_ms": execution_ms,
        "velocity": {"average": avg, "history": history, "trend": trend}
    }

def get_backlog_items(priority: str = "all", limit: int = 5) -> dict[str, Any]:
    t0 = time.perf_counter()
    data = _load()

    items = data["backlog"]
    if priority != "all":
        items = [i for i in items if i["priority"] == priority.lower()]

    items = sorted(items, key=lambda x: {"high": 0, "medium": 1, "low": 2}[x["priority"]])
    items = items[:limit]

    execution_ms = round((time.perf_counter() - t0) * 1000, 2)
    _log("get_backlog_items", execution_ms)

    return {
        "success": True,
        "execution_ms": execution_ms,
        "backlog": {
            "count": len(items),
            "filter": priority,
            "items": items,
            "total_points": sum(i["story_points"] for i in items)
        }
    }

def get_team_members(role: str = "all") -> dict[str, Any]:
    t0 = time.perf_counter()
    data = _load()

    members = data["team"]
    if role != "all":
        members = [m for m in members if role.lower() in m["role"].lower()]

    execution_ms = round((time.perf_counter() - t0) * 1000, 2)
    _log("get_team_members", execution_ms)

    return {
        "success": True,
        "execution_ms": execution_ms,
        "team": {"count": len(members), "members": members}
    }

def get_tasks_by_assignee_sprint(assignee: str) -> dict[str, Any]:
    t0 = time.perf_counter()
    data = _load()

    tasks = []
    for sprint in data["sprints"]:
        for task in sprint["tasks"]:
            if task["assignee"].lower() == assignee.lower():
                tasks.append({**task, "sprint": sprint["name"]})

    execution_ms = round((time.perf_counter() - t0) * 1000, 2)
    _log("get_tasks_by_assignee_sprint", execution_ms)

    if not tasks:
        return {"success": False, "message": f"No tasks found for: {assignee}"}

    return {
        "success": True,
        "execution_ms": execution_ms,
        "assignee": assignee,
        "count": len(tasks),
        "tasks": tasks
    }