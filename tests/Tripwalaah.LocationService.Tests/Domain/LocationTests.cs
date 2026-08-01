using FluentAssertions;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Tests.Domain;

public sealed class LocationTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var location = Location.Create(
            " Jaipur ",
            "Jaipur",
            "India",
            "in",
            26.9124,
            75.7873,
            LocationType.City,
            "Rajasthan",
            "Pink City",
            "Asia/Kolkata");

        location.Name.Should().Be("Jaipur");
        location.CountryCode.Should().Be("IN");
        location.Type.Should().Be(LocationType.City);
        location.IsActive.Should().BeTrue();
        location.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Create_WithInvalidCoordinates_Throws(double latitude, double longitude)
    {
        var act = () => Location.Create(
            "Invalid",
            "City",
            "Country",
            "XX",
            latitude,
            longitude,
            LocationType.City);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deactivate_MarksLocationInactive()
    {
        var location = Location.Create(
            "Dubai",
            "Dubai",
            "United Arab Emirates",
            "AE",
            25.2048,
            55.2708,
            LocationType.City);

        location.Deactivate();

        location.IsActive.Should().BeFalse();
        location.UpdatedAt.Should().NotBeNull();
    }
}
