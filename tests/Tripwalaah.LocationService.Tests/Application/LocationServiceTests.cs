using FluentAssertions;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;
using LocationAppService = Tripwalaah.LocationService.Application.Services.LocationAppService;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Tests.Application;

public sealed class LocationServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsAndReturnsLocation()
    {
        var repository = new InMemoryLocationRepository();
        var service = new LocationAppService(repository);

        var created = await service.CreateAsync(new CreateLocationRequest(
            "Changi Airport",
            "Singapore",
            "Singapore",
            "SG",
            1.3644,
            103.9915,
            LocationType.Airport));

        created.Name.Should().Be("Changi Airport");
        created.CountryCode.Should().Be("SG");
        created.Id.Should().NotBeNullOrWhiteSpace();

        var fetched = await service.GetByIdAsync(created.Id);
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task SearchAsync_FiltersByCountryCode()
    {
        var repository = new InMemoryLocationRepository();
        var service = new LocationAppService(repository);

        await service.CreateAsync(new CreateLocationRequest(
            "Jaipur", "Jaipur", "India", "IN", 26.9, 75.8, LocationType.City));
        await service.CreateAsync(new CreateLocationRequest(
            "Dubai", "Dubai", "United Arab Emirates", "AE", 25.2, 55.2, LocationType.City));

        var result = await service.SearchAsync(new LocationSearchRequest(CountryCode: "IN"));

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(x => x.Name == "Jaipur");
    }

    private sealed class InMemoryLocationRepository : ILocationRepository
    {
        private readonly List<Location> _locations = [];
        private int _counter;

        public Task<Location?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_locations.FirstOrDefault(x => x.Id == id));

        public Task<(IReadOnlyList<Location> Items, int TotalCount)> SearchAsync(
            string? query,
            string? countryCode,
            string? city,
            LocationType? type,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Location> queryable = _locations;

            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                var code = countryCode.Trim().ToUpperInvariant();
                queryable = queryable.Where(x => x.CountryCode == code);
            }

            if (isActive is not null)
            {
                queryable = queryable.Where(x => x.IsActive == isActive);
            }

            var filtered = queryable.OrderBy(x => x.Name).ToList();
            var pageItems = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(((IReadOnlyList<Location>)pageItems, filtered.Count));
        }

        public Task AddAsync(Location location, CancellationToken cancellationToken = default)
        {
            _counter++;
            location.AssignId($"507f1f77bcf86cd79943{_counter:D4}");
            _locations.Add(location);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Location location, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
