using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace AccountsAndPersons
{
    public class DapperDbContext
    {
        private readonly IConfiguration configuration;
        private readonly string connectionString;
        private readonly string databaseType;
        public DapperDbContext(IConfiguration configuration)
        {
            this.configuration = configuration;
            databaseType = configuration.GetConnectionString("DbType");
            connectionString = GetConnectionString();
        }
        private string GetConnectionString()
        {
            switch (databaseType)
            {
                case "PostgreSQL":
                    return this.configuration.GetConnectionString("PostgreSqlConnection");
                default:
                    return this.configuration.GetConnectionString("DefaultConnection");
            }
        }
        public IDbConnection CreateConnection()
        {
            switch (databaseType)
            {
                case "PostgreSQL":
                    return new NpgsqlConnection(connectionString);
                default:
                    return new NpgsqlConnection(connectionString);
            }
        }
    }
}
