using System;
using System.Collections.Generic;
using System.Text;

namespace SnpDepotManagement.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    protected Result(bool isSucess, Error error)
    {
        IsSuccess = isSucess;
        Error = error;
    }
    public static Result Success() => new Result(true, Error.None);
    public static Result Failure(Error error) => new Result(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}
    public class Result<T> : Result
    {
        public T? Value { get; }
        protected internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
        {
            Value = value;
        }
    }

