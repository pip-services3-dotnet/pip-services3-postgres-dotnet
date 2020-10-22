using PipServices3.Commons.Refer;
using PipServices3.Components.Build;
using PipServices3.Postgres.Persistence;

namespace PipServices3.Postgres.Build
{
    /// <summary>
    /// Creates PostgreSQL components by their descriptors.
    /// </summary>
    /// See <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/class_pip_services_1_1_components_1_1_build_1_1_factory.html">Factory</a>, 
    /// <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-postgres-dotnet/master/doc/api/class_pip_services_1_1_postgres_1_1_persistence_1_1_postgres_db_connection.html">PostgresConnection</a>
    public class DefaultPostgresFactory : Factory
    {
        public static Descriptor Descriptor = new Descriptor("pip-services", "factory", "postgres", "default", "1.0");
        public static Descriptor Descriptor3 = new Descriptor("pip-services3", "factory", "postgres", "default", "1.0");
        public static Descriptor PostgresConnection3Descriptor = new Descriptor("pip-services3", "connection", "postgres", "*", "1.0");
        public static Descriptor PostgresConnectionDescriptor = new Descriptor("pip-services", "connection", "postgres", "*", "1.0");

        /// <summary>
        /// Create a new instance of the factory.
        /// </summary>
        public DefaultPostgresFactory()
        {
            RegisterAsType(PostgresConnection3Descriptor, typeof(PostgresConnection));
            RegisterAsType(PostgresConnectionDescriptor, typeof(PostgresConnection));
        }
    }
}
