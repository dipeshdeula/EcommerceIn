namespace Application.Common
{
    public class Result<T>
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
        public T Data { get; set; }

        // pagination metadata (optional)
        public int? TotalCount { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public int? TotalPages { get; set; }
        public bool? HasNextPage { get; set; }
        public bool? HasPreviousPage { get; set; }

        /// <summary>
        /// Creates a success result with data and optional message
        /// </summary>
        public static Result<T> Success(T data, string message = "") =>
            new() { Succeeded = true, Message = message, Data = data };

        /// <summary>
        /// Creates a paginated success result
        /// </summary>
        public static Result<T> Success(T data, string message, int totalCount, int pageNumber, int pageSize)
        {
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            return new Result<T>
            {
                Succeeded = true,
                Message = message,
                Data = data,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = pageNumber < totalPages,
                HasPreviousPage = pageNumber > 1
            };
        }


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