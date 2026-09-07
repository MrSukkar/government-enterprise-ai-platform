namespace Platform.Knowledge.Retrieval;

public sealed class Neo4jEnterpriseGraphOptions
{
    public const string SectionName = "Neo4jEnterpriseGraph";

    public string Uri { get; init; } = string.Empty;
    public string Database { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public int QueryTimeoutSeconds { get; init; }
    public int MaximumRecords { get; init; }

    public Neo4jEnterpriseGraphConfigurationState ConfigurationState
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Uri) && string.IsNullOrWhiteSpace(Database) &&
                string.IsNullOrWhiteSpace(Username) && string.IsNullOrWhiteSpace(Password) &&
                QueryTimeoutSeconds == 0 && MaximumRecords == 0)
                return Neo4jEnterpriseGraphConfigurationState.Unconfigured;
            if (!System.Uri.TryCreate(Uri, UriKind.Absolute, out var endpoint) ||
                (endpoint.Scheme != "neo4j+s" && endpoint.Scheme != "bolt+s") ||
                !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) ||
                !string.IsNullOrEmpty(endpoint.Fragment) ||
                (endpoint.AbsolutePath.Length > 1 && endpoint.AbsolutePath != "/") ||
                string.IsNullOrWhiteSpace(Database) || string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password) || QueryTimeoutSeconds <= 0 || MaximumRecords <= 0)
                return Neo4jEnterpriseGraphConfigurationState.Invalid;
            return Neo4jEnterpriseGraphConfigurationState.Configured;
        }
    }

    public bool IsOperationallyConfigured =>
        ConfigurationState == Neo4jEnterpriseGraphConfigurationState.Configured;
}

public enum Neo4jEnterpriseGraphConfigurationState { Unconfigured, Invalid, Configured }

public sealed record Neo4jEnterpriseGraphReadiness(Neo4jEnterpriseGraphConfigurationState State)
{
    public bool IsOperationallyConfigured =>
        State == Neo4jEnterpriseGraphConfigurationState.Configured;
}
