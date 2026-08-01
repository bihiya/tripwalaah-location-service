using FluentAssertions;
using Tripwalaah.LocationService.Infrastructure.Realtime;

namespace Tripwalaah.LocationService.Tests.Realtime;

public sealed class InMemoryTripPresenceStoreTests
{
    [Fact]
    public async Task AddAndGetMembers_ReturnsJoinedUsers()
    {
        var store = new InMemoryTripPresenceStore();

        await store.AddOrUpdateAsync("trip-1", "user-a", "conn-a", "Alice");
        await store.AddOrUpdateAsync("trip-1", "user-b", "conn-b", "Bob");

        var members = await store.GetMembersAsync("trip-1");

        members.Should().HaveCount(2);
        members.Select(x => x.UserId).Should().BeEquivalentTo("user-a", "user-b");
        members.Should().OnlyContain(x => x.TripId == "trip-1");
    }

    [Fact]
    public async Task UpdateLocation_StoresLastKnownPosition()
    {
        var store = new InMemoryTripPresenceStore();
        await store.AddOrUpdateAsync("trip-1", "user-a", "conn-a", "Alice");

        var update = await store.UpdateLocationAsync("trip-1", "user-a", 26.9, 75.8, 40, 180);

        update.Should().NotBeNull();
        update!.Latitude.Should().Be(26.9);
        update.Longitude.Should().Be(75.8);

        var members = await store.GetMembersAsync("trip-1");
        members.Single().LastLocation.Should().NotBeNull();
        members.Single().LastLocation!.SpeedKmh.Should().Be(40);
    }

    [Fact]
    public async Task RemoveByConnection_RemovesMemberAndClearsEmptyTrip()
    {
        var store = new InMemoryTripPresenceStore();
        await store.AddOrUpdateAsync("trip-1", "user-a", "conn-a", "Alice");

        var removed = await store.RemoveByConnectionAsync("conn-a");

        removed.Should().NotBeNull();
        removed!.UserId.Should().Be("user-a");
        (await store.GetMembersAsync("trip-1")).Should().BeEmpty();
    }

    [Fact]
    public async Task Reconnect_ReplacesConnectionWithoutDuplicateMembers()
    {
        var store = new InMemoryTripPresenceStore();
        await store.AddOrUpdateAsync("trip-1", "user-a", "conn-old", "Alice");
        await store.AddOrUpdateAsync("trip-1", "user-a", "conn-new", "Alice");

        var members = await store.GetMembersAsync("trip-1");
        members.Should().ContainSingle();
        members.Single().ConnectionId.Should().Be("conn-new");
    }
}
