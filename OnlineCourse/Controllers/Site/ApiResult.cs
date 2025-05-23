﻿namespace OnlineCourse.Controllers.Site;

public class ApiResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
    public int StatusCode { get; set; }

    public ApiResult(bool success, string message, object data, int statusCode)
    {
        Success = success;
        Message = message;
        Data = data;
        StatusCode = statusCode;
    }
}

public class ApiResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public int StatusCode { get; set; }

    public ApiResult(bool success, string message, T data, int statusCode)
    {
        Success = success;
        Message = message;
        Data = data;
        StatusCode = statusCode;
    }
}