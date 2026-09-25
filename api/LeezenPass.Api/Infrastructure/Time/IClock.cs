namespace LeezenPass.Api.Infrastructure.Time;

public interface IClock
{
  DateTimeOffset UtcNow { get; }
}

public class SystemClock : IClock
{
  public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>Settable clock for tests. Not used by UseFakes: a frozen clock would break transfer expiry in the demo.</summary>
public class FakeClock(DateTimeOffset now) : IClock
{
  public DateTimeOffset UtcNow { get; set; } = now;

  public void Advance(TimeSpan by) => UtcNow += by;
}
