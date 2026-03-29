using MySql.Data.MySqlClient;

namespace ExamTest.Data;

public static class AppDb
{
    public static string ConnectionString =>
        "Server=127.0.0.1;Port=3306;Database=event_management_demo;Uid=root;Pwd=;SslMode=Disabled;";

    public static string ServerConnectionString =>
        "Server=127.0.0.1;Port=3306;Uid=root;Pwd=;SslMode=Disabled;";

    public static MySqlConnection CreateConnection()
    {
        return new MySqlConnection(ConnectionString);
    }

    public static MySqlConnection CreateServerConnection()
    {
        return new MySqlConnection(ServerConnectionString);
    }
}
