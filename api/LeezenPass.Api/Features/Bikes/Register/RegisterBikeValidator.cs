using FastEndpoints;

namespace LeezenPass.Api.Features.Bikes.Register;

public class RegisterBikeValidator : Validator<RegisterBikeRequest>
{
  public RegisterBikeValidator() => Include(new BikeDetailsValidator());
}
