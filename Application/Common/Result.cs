using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common
{
    public class Result
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public IEnumerable<string> Errors { get; set; } = new List<string>();

        public static Result Success(string message = "") => new() { Succeeded = true, Message = message };
        public static Result Failure(string message, IEnumerable<string> errors = null) =>
            new() { Succeeded = false, Message = message, Errors = errors ?? new List<string>() };
    }



}
