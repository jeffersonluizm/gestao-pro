namespace GestaoPro.API.Services;

public class VoteOperationException : Exception
{
    public int StatusCode { get; }

    public VoteOperationException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
