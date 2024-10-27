namespace Fluffy;

public class AppSettings
{
    public Logging Logging { get; set; }
    public TokenGeneration TokenGeneration { get; set; }
    public ConnectionStrings ConnectionStrings { get; set; }
    public OpenTelemetry OpenTelemetry { get; set; }
}

public class Logging
{
    public LogLevel LogLevel { get; set; }
}

public class LogLevel
{
    public string Default { get; set; }
    public string MicrosoftAspNetCore { get; set; }
}

public class TokenGeneration
{
    public string SecretKey { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
}

public class ConnectionStrings
{
    public string FluffyDbContext { get; set; }
}

public class OpenTelemetry
{
    public string Host { get; set; }
    public string Port { get; set; }
}