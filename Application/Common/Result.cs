using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common
{
    public class Result<T>
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public IEnumerable<string> Errors { get; set; } = new List<string>();
        public T Data { get; set; }

        public static Result<T> Success(T data, string message = "") => new() { Succeeded = true, Message = message, Data = data };
        public static Result<T> Failure(string message, IEnumerable<string> errors = null) =>
            new() { Succeeded = false, Message = message, Errors = errors ?? new List<string>() };
    }
}
