using Edge360.Application.Auth.Dtos;
using Edge360.Application.Driving.Dtos;
using Edge360.Application.Events.Dtos;
using Edge360.Application.Geofencing.Dtos;
using Edge360.Application.Groups.Dtos;
using Edge360.Application.Locations.Dtos;
using FluentValidation;

namespace Edge360.Application.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator() =>
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}

public sealed class JoinGroupRequestValidator : AbstractValidator<JoinGroupRequest>
{
    public JoinGroupRequestValidator() =>
        RuleFor(x => x.InviteCode).NotEmpty().Length(4, 16);
}

public sealed class RecordLocationRequestValidator : AbstractValidator<RecordLocationRequest>
{
    public RecordLocationRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.AccuracyMeters).GreaterThanOrEqualTo(0).When(x => x.AccuracyMeters.HasValue);
        RuleFor(x => x.SpeedMps).GreaterThanOrEqualTo(0).When(x => x.SpeedMps.HasValue);
        RuleFor(x => x.Heading).InclusiveBetween(0, 360).When(x => x.Heading.HasValue);
        RuleFor(x => x.BatteryLevel).InclusiveBetween(0, 100).When(x => x.BatteryLevel.HasValue);
    }
}

public sealed class CreatePlaceRequestValidator : AbstractValidator<CreatePlaceRequest>
{
    public CreatePlaceRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusMeters).GreaterThan(0).LessThanOrEqualTo(100_000);
    }
}

public sealed class UpdatePlaceRequestValidator : AbstractValidator<UpdatePlaceRequest>
{
    public UpdatePlaceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusMeters).GreaterThan(0).LessThanOrEqualTo(100_000);
    }
}

public sealed class AnalyzeDrivingRequestValidator : AbstractValidator<AnalyzeDrivingRequest>
{
    public AnalyzeDrivingRequestValidator()
    {
        RuleFor(x => x)
            .Must(r => !(r.From.HasValue && r.To.HasValue) || r.From <= r.To)
            .WithMessage("'From' must be on or before 'To'.");
    }
}

public sealed class SosRequestValidator : AbstractValidator<SosRequest>
{
    public SosRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.Message).MaximumLength(500);
    }
}
