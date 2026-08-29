using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GenomeTrack.Application.Response;

public class Detail
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public class Result
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; }

    [JsonIgnore]
    public bool IsNotFound { get; init; }

    [JsonIgnore]
    public bool IsConflict { get; init; }

    [JsonIgnore]
    public bool IsForbidden { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<Detail>? Details { get; init; }

    protected Result(
        bool isSuccess,
        string message,
        IReadOnlyList<Detail>? details,
        bool isNotFound = false,
        bool isConflict = false,
        bool isForbidden = false
    )
    {
        IsSuccess = isSuccess;
        Message = message;
        Details = details;
        IsNotFound = isNotFound;
        IsConflict = isConflict;
        IsForbidden = isForbidden;
    }

    public static Result Success(string message = "Success") => new(true, message, null);

    public static Result Failure(string message, List<Detail>? details = null) =>
        new(false, message, details);

    public static Result NotFound(string message, List<Detail>? details = null) =>
        new(false, message, details, isNotFound: true);

    public static Result Conflict(string message, List<Detail>? details = null) =>
        new(false, message, details, isConflict: true);

    public static Result Forbidden(string message, List<Detail>? details = null) =>
        new(false, message, details, isForbidden: true);
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    private Result(
        bool isSuccess,
        T? data,
        string message,
        IReadOnlyList<Detail>? details,
        bool isNotFound = false,
        bool isConflict = false,
        bool isForbidden = false
    )
        : base(isSuccess, message, details, isNotFound, isConflict, isForbidden)
    {
        Data = data;
    }

    public static Result<T> Success(T data, string message = "Success") =>
        new(true, data, message, null);

    public static new Result<T> Failure(string message, List<Detail>? details = null) =>
        new(false, default, message, details);

    public static new Result<T> NotFound(string message, List<Detail>? details = null) =>
        new(false, default, message, details, isNotFound: true);

    public static new Result<T> Conflict(string message, List<Detail>? details = null) =>
        new(false, default, message, details, isConflict: true);

    public static new Result<T> Forbidden(string message, List<Detail>? details = null) =>
        new(false, default, message, details, isForbidden: true);
}

public class Pagination
{
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class PagedData<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public Pagination Pagination { get; set; }

    public PagedData(IReadOnlyList<T> items, Pagination pagination)
    {
        Items = items;
        Pagination = pagination;
    }
}

/// <summary>Used only for Swagger ProducesResponseType documentation.</summary>
public class SuccessResult<T>
{
    public bool IsSuccess => true;
    public string Message { get; init; } = "Success";
    public T? Data { get; init; }
}

/// <summary>Used only for Swagger ProducesResponseType documentation.</summary>
public class FailureResult
{
    public bool IsSuccess => false;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<Detail>? Details { get; init; }
}
