namespace colosseumbackend.Tests;

[CollectionDefinition(Name)]
public class SolanaCollection : ICollectionFixture<SolanaTestFixture>
{
    public const string Name = "Solana";
}
