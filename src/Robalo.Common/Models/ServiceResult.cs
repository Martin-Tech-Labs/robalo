namespace Robalo.Common.Models;

public record ServiceError;
public record UnexpectedError;
public record ClientError
{
    ClientError(ClientErrorType errorType)
    {
        ErrorType = errorType;
    }
    
    public ClientErrorType ErrorType { get; }
    public enum ClientErrorType { Unauthorized, Forbidden, NotFound };

    public static ClientError AsUnauthorized() => new(ClientErrorType.Unauthorized);
    public static ClientError AsForbidden() => new(ClientErrorType.Forbidden);
    public static ClientError AsNotFound() => new(ClientErrorType.NotFound);
}

public readonly union ServiceResult<TResult>(ServiceError, UnexpectedError, TResult) where TResult : notnull;
public readonly union ServiceResult<TResult, TAppError>(TAppError, ServiceError, UnexpectedError, TResult) where TResult : notnull where TAppError : notnull;