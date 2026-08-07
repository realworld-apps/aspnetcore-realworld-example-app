namespace Conduit.Infrastructure.Errors;

public static class Constants
{
    public const string NOT_FOUND = "not found";
    public const string IN_USE = "has already been taken";
    public const string BLANK = "can't be blank";
    public const string FORBIDDEN = "forbidden";
    public const string PASSWORD_TOO_SHORT = "is too short (minimum is 8 characters)";
    public const string InternalServerError = nameof(InternalServerError);
}
