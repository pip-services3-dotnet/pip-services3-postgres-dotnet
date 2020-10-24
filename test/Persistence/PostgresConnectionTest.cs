using PipServices3.Commons.Config;
using System;
using Xunit;

namespace PipServices3.Postgres.Persistence
{
    /// <summary>
    /// Unit tests for the <c>PostgresConnectionTest</c> class
    /// </summary>
    [Collection("Sequential")]
    public class PostgresConnectionTest: IDisposable
    {
        private PostgresConnection Db { get; }

        private string postgresUri;
        private string postgresHost;
        private string postgresPort;
        private string postgresDatabase;
        private string postgresUsername;
        private string postgresPassword;

        public PostgresConnectionTest()
        {
            Db = new PostgresConnection();

            postgresUri = Environment.GetEnvironmentVariable("POSTGRES_URI");
            postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
            postgresPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
            postgresDatabase = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "test";
            postgresUsername = Environment.GetEnvironmentVariable("POSTGRES_USERNAME") ?? "postgres";
            postgresPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
            if (postgresUri == null && postgresHost == null)
                return;

            if (Db == null) return;
        }

        public void Dispose()
        {
            Db.CloseAsync(null).Wait();
        }

        [Fact]
        public void TestOpenAsync_Success()
        {
            Db.Configure(ConfigParams.FromTuples(
                "connection.uri", postgresUri,
                "connection.host", postgresHost,
                "connection.port", postgresPort,
                "connection.database", postgresDatabase,
                "credential.username", postgresUsername,
                "credential.password", postgresPassword
            ));

            Db.OpenAsync(null).Wait();

            var actual = Db.IsOpen();

            Assert.True(actual);
        }

        //[Fact]
        //public void TestOpenAsync_Failure()
        //{
        //    Db.CloseAsync(null).Wait();

        //    Db.Configure(ConfigParams.FromTuples(
        //        "connection.uri", postgresUri,
        //        "connection.host", postgresHost,
        //        "connection.port", "1234",
        //        "connection.database", postgresDatabase
        //    ));

        //    var ex = Assert.Throws<AggregateException>(() => Db.OpenAsync(null).Wait());
        //    Assert.Equal("Connection to postgres failed", ex.InnerException.Message);
        //}
    }
}
