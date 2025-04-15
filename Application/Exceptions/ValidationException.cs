using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Exceptions
{
    public class ValidationException : Exception
    {
        public ValidationException() : base("One or more validation failures have occurred.")
        {
            // Errors = new List<string>();
            Errors = new Dictionary<string, string[]>();
        }
        //  public List<string> Errors { get; }
        public ValidationException(IEnumerable<ValidationFailure> failures)
            : this()
        {
            //foreach (var failure in failures)
            //{
            //    Errors.Add(failure.ErrorMessage);
            //}
            var failureGroups = failures
               .GroupBy(e => e.PropertyName, e => e.ErrorMessage);

            foreach (var failureGroup in failureGroups)
            {
                var propertyName = failureGroup.Key;
                var propertyFailures = failureGroup.ToArray();

                Errors.Add(propertyName, propertyFailures);
            }
        }
        public IDictionary<string, string[]> Errors { get; }
    }
}
