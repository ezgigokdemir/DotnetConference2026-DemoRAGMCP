using Microsoft.AspNetCore.Mvc;
using PmRagChatbot.Services;

namespace PmRagChatbot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly RagService _ragService;

    public ChatController(RagService ragService)
    {
        _ragService = ragService;
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question cannot be empty.");

        var response = await _ragService.AskAsync(request.Question);
        return Ok(response);
    }
}

public record AskRequest(string Question);