namespace Commerce.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class CommerceApiCollection : ICollectionFixture<CommerceApiFactory>
{
    public const string Name = "Commerce API";
}
