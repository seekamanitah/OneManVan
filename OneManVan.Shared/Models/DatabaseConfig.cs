namespace OneManVan.Shared.Models;

/// <summary>
/// Database connection configuration model.
/// Supports both SQLite (local) and SQL Server (remote) connections.
/// </summary>
public class DatabaseConfig
{
    /// <summary>
    /// Type of database (SQLite or SQL Server)
    /// </summary>
    public DatabaseType Type { get; set; } = DatabaseType.SQLite;

    /// <summary>
    /// SQLite file path (relative or absolute)
    /// </summary>
    public string SqliteFilePath { get; set; } = "OneManVan.db";

    /// <summary>
    /// SQL Server address (hostname or IP)
    /// </summary>
    public string? ServerAddress { get; set; }

    /// <summary>
    /// SQL Server port (default: 1433)
    /// </summary>
    public int ServerPort { get; set; } = 1433;

    /// <summary>
    /// Database name on SQL Server
    /// </summary>
    public string DatabaseName { get; set; } = "OneManVanFSM";

    /// <summary>
    /// SQL Server username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// SQL Server password (encrypted in storage)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Trust server certificate (for self-signed certs)
    /// </summary>
    public bool TrustServerCertificate { get; set; } = true;

    /// <summary>
    /// Use encryption for connection
    /// </summary>
    public bool Encrypt { get; set; } = false;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Gets the full connection string based on configuration
    /// </summary>
    public string GetConnectionString(string? basePathForSqlite = null)
    {
        if (Type == DatabaseType.SQLite)
        {
            var path = SqliteFilePath;
            if (!string.IsNullOrEmpty(basePathForSqlite) && !Path.IsPathRooted(path))
            {
                path = Path.Combine(basePathForSqlite, path);
            }
            return $"Data Source={path}";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ServerAddress))
                throw new InvalidOperationException("Server address is required for SQL Server connection");

            return $"Server={ServerAddress},{ServerPort};" +
                   $"Database={DatabaseName};" +
                   $"User Id={Username};" +
                   $"Password={Password};" +
                   $"TrustServerCertificate={TrustServerCertificate};" +
                   $"Encrypt={Encrypt};" +
                   $"Connection Timeout={ConnectionTimeout};";
        }
    }

    /// <summary>
    /// Gets a display-friendly connection string (with masked password)
    /// </summary>
    public string GetDisplayConnectionString(string? basePathForSqlite = null)
    {
        if (Type == DatabaseType.SQLite)
        {
            return GetConnectionString(basePathForSqlite);
        }
        else
        {
            return $"Server={ServerAddress},{ServerPort};" +
                   $"Database={DatabaseName};" +
                   $"User Id={Username};" +
                   $"Password=********;" +
                   $"TrustServerCertificate={TrustServerCertificate};" +
                   $"Encrypt={Encrypt};";
        }
    }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public (bool IsValid, string ErrorMessage) Validate()
    {
        if (Type == DatabaseType.SQLite)
        {
            if (string.IsNullOrWhiteSpace(SqliteFilePath))
                return (false, "SQLite file path is required");

            return (true, string.Empty);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ServerAddress))
                return (false, "Server address is required");

            if (ServerPort < 1 || ServerPort > 65535)
                return (false, "Server port must be between 1 and 65535");

            if (string.IsNullOrWhiteSpace(DatabaseName))
                return (false, "Database name is required");

            if (string.IsNullOrWhiteSpace(Username))
                return (false, "Username is required");

            if (string.IsNullOrWhiteSpace(Password))
                return (false, "Password is required");

            return (true, string.Empty);
        }
    }

    /// <summary>
    /// Creates a default configuration
    /// </summary>
    public static DatabaseConfig CreateDefault()
    {
        return new DatabaseConfig
        {
            Type = DatabaseType.SQLite,
            SqliteFilePath = "OneManVan.db"
        };
    }
}

/// <summary>
/// Database type enumeration
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// Local SQLite database (offline, standalone)
    /// </summary>
    SQLite = 0,

    /// <summary>
    /// Remote SQL Server database (Docker, Cloud, or on-premise)
    /// </summary>
    SqlServer = 1
}
