namespace RedPandaFlow.Application.Common
{
    public enum ServiceErrorType
    {
        None,
        Validation,
        NotFound,
        Forbidden,
        Conflict
    }

    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public ServiceErrorType ErrorType { get; set; } = ServiceErrorType.None;

        public static ServiceResult<T> Ok(T data, string message = "") => new()
        {
            Success = true,
            Data = data,
            Message = message
        };

        public static ServiceResult<T> Fail(string message, ServiceErrorType errorType) => new()
        {
            Success = false,
            Message = message,
            ErrorType = errorType
        };
    }
}
