namespace aspnet_core_tutorial.IntegrationTests.Infrastructure;

/// <summary>
/// Groups every integration test class under a single xUnit collection so they all share one
/// <see cref="IntegrationTestWebApplicationFactory"/> instance, and therefore one PostgreSQL
/// Testcontainer, instead of each class paying the cost of starting its own container. Tests in
/// the same collection run sequentially, which also keeps CRUD tests that mutate the shared
/// database from racing each other.
/// </summary>
/// <remarks>
/// Created by edgar.muhamyangabo on 7/13/26
/// Author : edgar.muhamyangabo
/// Date : 7/13/26
/// Project : aspnet_core_tutorial
/// </remarks>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebApplicationFactory>
{
    public const string Name = "Integration Tests";
}
