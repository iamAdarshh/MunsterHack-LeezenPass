using FastEndpoints;

namespace LeezenPass.Api.Features.Bikes.Update;

public class UpdateBikeValidator : Validator<UpdateBikeRequest>
{
  public UpdateBikeValidator() => Include(new BikeDetailsValidator());
}
