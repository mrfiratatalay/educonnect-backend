using EduConnect.Application.Contracts.Feedbacks;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduConnect.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class FeedbacksController(AppDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var feedback = new Feedback
        {
            UserId = userId.Value,
            FeatureArea = request.FeatureArea.Trim(),
            Rating = request.Rating,
            Comment = request.Comment?.Trim()
        };

        dbContext.Feedbacks.Add(feedback);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/feedbacks/{feedback.Id}", new { feedback.Id });
    }
}
