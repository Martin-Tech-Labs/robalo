namespace Robalo.Controller.Api.Cli;

public static class Exitcodes
{
    /// <summary>
    /// Command completed successfully
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Provided data are invalid or not supported
    /// </summary>
    public const int InputError = 2;

    /// <summary>
    /// Command was cancelled by a caller, e.g. using Ctrl+C
    /// </summary>
    public const int Canceled = 130;

    /// <summary>
    /// User replied "n" to a confirmation prompt
    /// </summary>
    public const int ConfirmationRefused = 3;

    /// <summary>
    /// Command is not supported with the current state of the application.
    /// </summary>
    public const int CommandNotSupported = 4;

    /// <summary>
    /// Unspecified error occurred during comman execution
    /// </summary>
    public const int UnexpectedError = 5;
}