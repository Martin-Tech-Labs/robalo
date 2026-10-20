namespace Robalo.Common.Models;

public record ServiceError;
public record UnexpectedError;
public record AuthError
{
    AuthError(AuthErrorType errorType)
    {
        ErrorType = errorType;
    }
    
    public AuthErrorType ErrorType { get; }
    public enum AuthErrorType { Unauthorized, Forbidden };

    public static AuthError AsUnauthorized() => new(AuthErrorType.Unauthorized);
    public static AuthError AsForbidden() => new(AuthErrorType.Forbidden);
}

public readonly union ServiceResult<TResult>(ServiceError, UnexpectedError, TResult) where TResult : notnull;
public readonly union ServiceResult<TResult, TAppError>(TAppError, ServiceError, UnexpectedError, TResult) where TResult : notnull where TAppError : notnull;