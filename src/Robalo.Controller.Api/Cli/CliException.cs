namespace Robalo.Controller.Api;

public sealed class CliException : Exception
{
    public int ExitCode { get; }

    public CliException(int exitCode, string message) : base(message)
    {
        ExitCode = exitCode;
    }

    public CliException(int exitCode, string message, Exception innerException) : base(message, innerException)
    {
        ExitCode = exitCode;
    }
}