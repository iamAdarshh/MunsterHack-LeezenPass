using FastEndpoints;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Verification;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Verification.Challenge;

public sealed record CreateChallengeRequest
{
  public Guid Id { get; init; }
}

/// <summary>The plain code is only in this response; it is stored as a hash bound to bike and user.</summary>
public sealed record CreateChallengeResponse(string Code, DateTimeOffset ExpiresAt);

public class CreateChallengeEndpoint(AppDbContext db, IClock clock) : Endpoint<CreateChallengeRequest, CreateChallengeResponse>
{
  public override void Configure()
  {
    Post("bikes/{id}/verification/challenge");
    Options(x => x.RequireRateLimiting(RateLimitPolicies.Challenge));
    Summary(s => s.Summary = "New 4-digit possession code (10 min, single use)");
  }

  public override async Task HandleAsync(CreateChallengeRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    if (!await db.Bikes.AnyAsync(b => b.Id == req.Id && b.OwnerId == userId, ct))
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var (challenge, code) = PossessionChallenge.Create(req.Id, userId, clock.UtcNow);
    db.PossessionChallenges.Add(challenge);
    await db.SaveChangesAsync(ct);
    await Send.ResponseAsync(new CreateChallengeResponse(code, challenge.ExpiresAt), StatusCodes.Status201Created, ct);
  }
}
