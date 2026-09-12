namespace AgriCheck.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<ApiError>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Fail(params ApiError[] errors) => new() { Success = false, Errors = errors };
    public static ApiResponse<T> Fail(string code, string message) => Fail(new ApiError(code, message));
}

public record ApiError(string Code, string Message);
