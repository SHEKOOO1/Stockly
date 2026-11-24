using Stockly.StocklyDbTestSupport;
using Xunit;

// xUnit requires the [CollectionDefinition] to live in the SAME assembly as the tests
// that use it (xUnit1041). The fixture implementation is shared so there is exactly
// one copy; this file is linked into each database-dependent test assembly so each one
// declares its own collection over that single fixture type.
[CollectionDefinition(SqlServerCollection.Name)]
public sealed class SqlServerTestCollection : ICollectionFixture<SqlServerFixture>
{
}