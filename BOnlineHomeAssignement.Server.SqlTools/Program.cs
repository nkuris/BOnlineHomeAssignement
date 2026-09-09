using System;
using Microsoft.Data.SqlClient;

try
{
    var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? (args.Length > 0 ? args[0] : null);
    if (string.IsNullOrEmpty(conn))
    {
        Console.Error.WriteLine("Connection string not provided via ConnectionStrings__DefaultConnection or first argument.");
        Environment.Exit(2);
    }

    Console.WriteLine($"Testing SQL connection using connection string: {conn}");

    using var connObj = new SqlConnection(conn);
    connObj.Open();
    using var cmd = connObj.CreateCommand();
    cmd.CommandText = "SELECT 1";
    var result = cmd.ExecuteScalar();
    Console.WriteLine($"Query result: {result}");
    Console.WriteLine("SQL login successful");
    Environment.Exit(0);
}
catch (Exception ex)
{
    Console.Error.WriteLine("SQL connection test failed: " + ex.Message);
    Environment.Exit(1);
}
