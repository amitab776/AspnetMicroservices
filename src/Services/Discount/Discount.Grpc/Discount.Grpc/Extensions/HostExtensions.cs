using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Discount.Grpc.Extensions
{
    public static class HostExtensions
    {
        public static IHost MigratePostgreData<TContext>(this IHost host, int? maxRetries = 0)
        {
            int retryForAvailability = maxRetries.Value;
            using(var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILogger<TContext>>();

                try
                {
                    logger.LogInformation("Migrating postgre Sql database");
                    using var connection = new NpgsqlConnection(configuration.GetValue<string>("DabaseSettings:ConnectionString"));
                    connection.Open();

                    using var command = new NpgsqlCommand
                    {
                        Connection = connection
                    };

                    command.CommandText = $"DROP TABLE IF EXISTS Coupon";
                    command.ExecuteNonQuery();

                    command.CommandText = $"CREATE TABLE Coupon(ID SERIAL PRIMARY KEY NOT NULL," +
                                          $" ProductName VARCHAR(24) NOT NULL, " +
                                          $"Description TEXT, Amount INT); ";
                    command.ExecuteNonQuery();

                    command.CommandText = $"INSERT INTO Coupon (ProductName, Description, Amount) VALUES ('IPhone X', 'IPhone Discount', 150);" +
                                          $"INSERT INTO Coupon(ProductName, Description, Amount) VALUES('Samsung 10', 'Samsung Discount', 100); ";

                    command.ExecuteNonQuery();

                    logger.LogInformation("Migration of Postgre Sql database complete");
                }
                catch (NpgsqlException ex)
                {
                    logger.LogError("There is an exception while migrating the database");

                    if(retryForAvailability < 50)
                    {
                        retryForAvailability++;
                        System.Threading.Thread.Sleep(2000);
                        MigratePostgreData<TContext>(host, retryForAvailability);
                    }
                }
            }
            return host;
        }
    }
}
