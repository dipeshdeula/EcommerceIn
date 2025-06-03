/*using System;
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
}*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Common
{
    public class Result<T>
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
        public T Data { get; set; }

        /// <summary>
        /// Creates a success result with data and optional message
        /// </summary>
        public static Result<T> Success(T data, string message = "") =>
            new() { Succeeded = true, Message = message, Data = data };

        /// <summary>
        /// Creates a failure result with a message and property-specific errors
        /// </summary>
        public static Result<T> Failure(string message, IDictionary<string, string[]> errors) =>
            new() { Succeeded = false, Message = message, Errors = errors };

        /// <summary>
        /// Creates a failure result with a message and simple list of errors
        /// </summary>
        public static Result<T> Failure(string message, IEnumerable<string> errors = null)
        {
            var result = new Result<T> { Succeeded = false, Message = message };

            if (errors != null && errors.Any())
            {
                // Convert simple error list to dictionary format with "General" as the key
                result.Errors = new Dictionary<string, string[]>
                {
                    { "General", errors.ToArray() }
                };
            }

            return result;
        }

        /// <summary>
        /// Creates a failure result with a message and a single error
        /// </summary>
        public static Result<T> Failure(string message, string error)
        {
            return Failure(message, new[] { error });
        }

        /// <summary>
        /// Creates a failure result for a specific property
        /// </summary>
        public static Result<T> PropertyFailure(string message, string propertyName, params string[] errors)
        {
            var result = new Result<T> { Succeeded = false, Message = message };

            result.Errors = new Dictionary<string, string[]>
            {
                { propertyName, errors }
            };

            return result;
        }
    }
}