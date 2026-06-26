using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Edge360.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Edge360.Domain.Tests;

public class PlaceAndGroupTests
{
    private static Place HomePlace() => new()
    {
        Name = "Home",
        Latitude = 40.0,
        Longitude = -74.0,
        RadiusMeters = 150
    };

    [Fact]
    public void Place_Contains_RespectsRadius()
    {
        var place = HomePlace();
        place.Contains(new GeoCoordinate(40.0, -74.0)).Should().BeTrue();
        place.Contains(new GeoCoordinate(41.0, -74.0)).Should().BeFalse();
    }

    [Theory]
    [InlineData(false, true, PlaceTransition.Entered)]
    [InlineData(true, false, PlaceTransition.Exited)]
    [InlineData(true, true, PlaceTransition.None)]
    [InlineData(false, false, PlaceTransition.None)]
    public void Place_Evaluate_ClassifiesTransition(bool wasInside, bool placeInside, PlaceTransition expected)
    {
        var place = HomePlace();
        // Choose a coordinate inside or outside to drive the "current" state.
        var current = placeInside ? new GeoCoordinate(40.0, -74.0) : new GeoCoordinate(41.0, -74.0);

        place.Evaluate(wasInside, current).Should().Be(expected);
    }

    [Fact]
    public void Group_GenerateInviteCode_HasRequestedLengthAndSafeAlphabet()
    {
        var code = Group.GenerateInviteCode(8);
        code.Should().HaveLength(8);
        code.Should().MatchRegex("^[A-HJ-NP-Z2-9]+$"); // no 0/O/1/I
    }

    [Fact]
    public void Group_GenerateInviteCode_IsRandom()
    {
        var codes = Enumerable.Range(0, 50).Select(_ => Group.GenerateInviteCode()).ToHashSet();
        codes.Count.Should().BeGreaterThan(40); // overwhelmingly unique
    }

    [Fact]
    public void RefreshToken_Revoke_DeactivatesToken()
    {
        var token = new RefreshToken { TokenHash = "x", ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        token.IsActive.Should().BeTrue();

        token.Revoke();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void GroupMembership_SharingPaused_IsNotActive()
    {
        var m = new GroupMembership { LocationSharingEnabled = true, SharingPausedUntil = DateTimeOffset.UtcNow.AddHours(1) };
        m.IsSharingActive.Should().BeFalse();
    }
}
