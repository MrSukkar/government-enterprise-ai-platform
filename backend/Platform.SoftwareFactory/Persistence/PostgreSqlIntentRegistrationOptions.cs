using Npgsql;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlIntentRegistrationOptions
{
    public const string SectionName = "PostgreSqlIntentRegistration";

    public string ConnectionString { get; init; } = string.Empty;
    public int CommandTimeoutSeconds { get; init; }

    public PostgreSqlIntentRegistrationConfigurationState ConfigurationState
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ConnectionString) && CommandTimeoutSeconds == 0)
                return PostgreSqlIntentRegistrationConfigurationState.Unconfigured;
            if (string.IsNullOrWhiteSpace(ConnectionString) || CommandTimeoutSeconds <= 0)
                return PostgreSqlIntentRegistrationConfigurationState.Invalid;
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(ConnectionString);
                if (string.IsNullOrWhiteSpace(builder.Host) || string.IsNullOrWhiteSpace(builder.Database) ||
                    string.IsNullOrWhiteSpace(builder.Username) || builder.SslMode != SslMode.VerifyFull ||
                    builder.IncludeErrorDetail)
                    return PostgreSqlIntentRegistrationConfigurationState.Invalid;
            }
            catch (ArgumentException)
            {
                return PostgreSqlIntentRegistrationConfigurationState.Invalid;
            }
            return PostgreSqlIntentRegistrationConfigurationState.Configured;
        }
    }

    public bool IsOperationallyConfigured =>
        ConfigurationState == PostgreSqlIntentRegistrationConfigurationState.Configured;
}

public enum PostgreSqlIntentRegistrationConfigurationState { Unconfigured, Invalid, Configured }

public sealed record PostgreSqlIntentRegistrationReadiness(
    PostgreSqlIntentRegistrationConfigurationState State)
{
    public bool IsOperationallyConfigured =>
        State == PostgreSqlIntentRegistrationConfigurationState.Configured;
}
