﻿#nullable enable
using Newtonsoft.Json;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace TotallyWholesome.Managers.Shockers.OpenShock.Models;

public class BaseResponse<T>
{
    public string? Message { get; set; }
    public T? Data { get; set; }

    public BaseResponse(string? message = null, T? data = default)
    {
        Message = message;
        Data = data;
    }
    
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}