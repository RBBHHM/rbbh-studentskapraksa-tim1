using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.RelatedPersons;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using RBBH.ConnectedParties.DL.Persistence;
using UnitTests.Mocks.DB;

namespace UnitTests.RelatedPersons;

public sealed class RelationshipTreeContractTests
{
    private static RelatedPersonService Service(ConnectedPartiesDbContext db) => new(
        db, Mock.Of<IEmailService>(), Options.Create(new EmailSettings()));

    [Fact]
    public async Task SelectedDescendantReturnsAncestorAndOrderedActiveBranches()
    {
        await using var db = InMemoryContextFactory.Create();
        var root = new RelatedPerson { FirstName = "Root", IsIdentifiedStaff = true };
        var first = new RelatedPerson { FirstName = "Amir", RelatedToPersonId = root.Id };
        var last = new RelatedPerson { FirstName = "Zoran", RelatedToPersonId = root.Id };
        var inactive = new RelatedPerson { FirstName = "Hidden", RelatedToPersonId = root.Id, IsActive = false };
        db.RelatedPersons.AddRange(root, last, inactive, first);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var result = await Service(db).GetRelationshipTree(first.Id);
        Assert.True(result.IsSuccessful);
        var node = Assert.Single(result.Value);
        Assert.Equal(root.Id, node.Id);
        Assert.Equal("Employee", node.PersonType);
        Assert.Equal("Zaposlenik", node.PersonTypeLabel);
        Assert.Equal(new[] { first.Id, last.Id }, node.Children.Select(child => child.Id));
        Assert.All(node.Children, child => Assert.Equal("FamilyMember", child.PersonType));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ExistingCycleEndsWithRepeatedLeafInsteadOfRecursingForever()
    {
        await using var db = InMemoryContextFactory.Create();
        var first = new RelatedPerson { FirstName = "First" };
        var second = new RelatedPerson { FirstName = "Second", RelatedToPersonId = first.Id };
        first.RelatedToPersonId = second.Id;
        db.RelatedPersons.AddRange(first, second);
        await db.SaveChangesAsync();
        var result = await Service(db).GetRelationshipTree(first.Id);
        var root = Assert.Single(Assert.IsType<List<RelatedPersonTreeNodeDTO>>(result.Value));
        Assert.Equal(first.Id, root.Id);
        var child = Assert.Single(root.Children);
        Assert.Equal(second.Id, child.Id);
        var leaf = Assert.Single(child.Children);
        Assert.Equal(first.Id, leaf.Id);
        Assert.Empty(leaf.Children);
    }

    [Fact]
    public async Task InactiveParentLeavesSelectedActivePersonAsRoot()
    {
        await using var db = InMemoryContextFactory.Create();
        var parent = new RelatedPerson { IsActive = false };
        var child = new RelatedPerson { RelatedToPersonId = parent.Id };
        db.RelatedPersons.AddRange(parent, child);
        await db.SaveChangesAsync();
        var result = await Service(db).GetRelationshipTree(child.Id);
        var root = Assert.Single(Assert.IsType<List<RelatedPersonTreeNodeDTO>>(result.Value));
        Assert.Equal(child.Id, root.Id);
        Assert.Equal("FamilyMember", root.PersonType);
        Assert.Empty(root.Children);
        Assert.True((await Service(db).GetRelationshipTree(parent.Id)).IsNotFoundError);
    }
}
