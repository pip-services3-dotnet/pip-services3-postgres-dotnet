using PipServices3.Commons.Config;
using PipServices3.Postgres.Fixtures;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PipServices3.Postgres.Persistence
{
    /// <summary>
    /// Unit tests for the <c>PostgresPersistenceTest2</c> class
    /// </summary>
    [Collection("Sequential")]
    public class PostgresPersistenceTest2: IDisposable
    {
        private PostgresDummyPersistence2 persistence;
        private DummyPersistenceFixture2 fixture;

        private string postgresUri;
        private string postgresHost;
        private string postgresPort;
        private string postgresDatabase;
        private string postgresUsername;
        private string postgresPassword;

        public PostgresPersistenceTest2()
        {
            postgresUri = Environment.GetEnvironmentVariable("POSTGRES_URI");
            postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
            postgresPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
            postgresDatabase = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "test";
            postgresUsername = Environment.GetEnvironmentVariable("POSTGRES_USERNAME") ?? "postgres";
            postgresPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
            
            if (postgresUri == null && postgresHost == null)
                return;

            var dbConfig = ConfigParams.FromTuples(
                "connection.uri", postgresUri,
                "connection.host", postgresHost,
                "connection.port", postgresPort,
                "connection.database", postgresDatabase,
                "credential.username", postgresUsername,
                "credential.password", postgresPassword
            );

            persistence = new PostgresDummyPersistence2();
            persistence.Configure(dbConfig);

            fixture = new DummyPersistenceFixture2(persistence);

            persistence.OpenAsync(null).Wait();
            persistence.ClearAsync(null).Wait();
        }

        public void Dispose()
        {
            persistence.CloseAsync(null).Wait();
        }

        [Fact]
        public async Task TestCrudOperations()
        {
            await fixture.TestCrudOperationsAsync();
        }

        [Fact]
        public async Task TestBatchOperations()
        {
            await fixture.TestBatchOperationsAsync();
        }

    }
}
