using Application.Common;
using System.Globalization;

namespace Application.Common.Helper
{
    public static class TimeParsingHelper
    {
        /// <summary>
        /// Parse flexible datetime input supporting multiple formats
        /// </summary>
        public static Result<DateTime> ParseFlexibleDateTime(string input, DateTime? baseDate = null)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Result<DateTime>.Failure("Time input cannot be empty");

            input = input.Trim();
            var targetDate = baseDate ?? DateTime.Today;

            //  FULL DATETIME FORMATS
            var fullDateTimeFormats = new[]
            {
                "yyyy-MM-dd h:mm tt",      // "2025-06-11 1:20 PM"
                "yyyy-MM-dd hh:mm tt",     // "2025-06-11 01:20 PM"
                "yyyy-MM-dd H:mm",         // "2025-06-11 13:20"
                "yyyy-MM-dd HH:mm",        // "2025-06-11 13:20"
                "yyyy-MM-dd h:mm:ss tt",   // "2025-06-11 1:20:00 PM"
                "yyyy-MM-dd HH:mm:ss",     // "2025-06-11 13:20:00"
                "yyyy-MM-ddTHH:mm:ss",     // "2025-06-11T13:20:00"
                "yyyy-MM-dd HH:mm:ss.fff", // "2025-06-11 13:20:00.000"
                "MM/dd/yyyy h:mm tt",      // "06/11/2025 1:20 PM"
                "dd/MM/yyyy H:mm",         // "11/06/2025 13:20"
            };

            foreach (var format in fullDateTimeFormats)
            {
                if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var fullDateTime))
                {
                    return Result<DateTime>.Success(fullDateTime);
                }
            }

            // TIME ONLY FORMATS (use baseDate)
            var timeOnlyFormats = new[]
            {
                "h:mm tt",      // "1:20 PM"
                "hh:mm tt",     // "01:20 PM"
                "H:mm",         // "13:20"
                "HH:mm",        // "13:20"
                "h:mm:ss tt",   // "1:20:00 PM"
                "HH:mm:ss"      // "13:20:00"
            };

            foreach (var format in timeOnlyFormats)
            {
                if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var timeOnly))
                {
                    var combinedDateTime = targetDate.Date.Add(timeOnly.TimeOfDay);
                    return Result<DateTime>.Success(combinedDateTime);
                }
            }

            //  NATURAL LANGUAGE PARSING (fallback)
            if (DateTime.TryParse(input, out var naturalParsed))
            {
                return Result<DateTime>.Success(naturalParsed);
            }

            return Result<DateTime>.Failure($"Unable to parse time format: '{input}'. " +
                "Supported formats: '1:20 PM', '13:20', '2025-06-11 1:20 PM', etc.");
        }

        /// <summary>
        /// Parse time slot string like "1:20 PM - 2:30 PM" or "13:20-14:30"
        /// </summary>
        public static Result<(DateTime start, DateTime end)> ParseTimeSlot(string timeSlot, DateTime baseDate)
        {
            if (string.IsNullOrWhiteSpace(timeSlot))
                return Result<(DateTime, DateTime)>.Failure("Time slot cannot be empty");

            var separators = new[] { "-", "to", "until", "—" };
            string[] parts = null;

            foreach (var separator in separators)
            {
                if (timeSlot.Contains(separator))
                {
                    parts = timeSlot.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                    break;
                }
            }

            if (parts == null || parts.Length != 2)
                return Result<(DateTime, DateTime)>.Failure(
                    "Invalid time slot format. Use: '1:20 PM - 2:30 PM' or '13:20-14:30'");

            var startResult = ParseFlexibleDateTime(parts[0].Trim(), baseDate);
            var endResult = ParseFlexibleDateTime(parts[1].Trim(), baseDate);

            if (!startResult.Succeeded)
                return Result<(DateTime, DateTime)>.Failure($"Invalid start time: {startResult.Errors}");

            if (!endResult.Succeeded)
                return Result<(DateTime, DateTime)>.Failure($"Invalid end time: {endResult.Errors}");

            return Result<(DateTime, DateTime)>.Success((startResult.Data, endResult.Data));
        }

        /// <summary>
        /// Get suggested time formats for user guidance
        /// </summary>
        public static List<string> GetSupportedFormats()
        {
            return new List<string>
            {
                "2025-06-11 1:20 PM",
                "2025-06-11 13:20",
                "1:20 PM",
                "13:20:00",
                "2025-06-11T13:20:00"
            };
        }

        /// <summary>
        /// Format datetime for Nepal time display
        /// </summary>
        public static string FormatForNepalDisplay(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd h:mm tt"); // "2025-06-11 1:20 PM"
        }

        /// <summary>
        /// Validate if datetime string is in valid format without parsing
        /// </summary>
        public static bool IsValidDateTimeFormat(string input)
        {
            var result = ParseFlexibleDateTime(input);
            return result.Succeeded;
        }

        /// <summary>
        /// Get user-friendly error message with suggestions
        /// </summary>
        public static string GetParsingErrorMessage(string input)
        {
            return $"Unable to parse '{input}'. Try formats like: {string.Join(", ", GetSupportedFormats())}";
        }
                /// <summary>
        /// Parse Nepal timezone datetime input with enhanced format support
        /// </summary>
        public static Result<DateTime> ParseNepalDateTime(string input, DateTime? baseDate = null)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Result<DateTime>.Failure("Time input cannot be empty");

            input = input.Trim();
            var targetDate = baseDate ?? DateTime.Today;

            //  NEPAL-SPECIFIC DATETIME FORMATS (prioritized)
            var nepalDateTimeFormats = new[]
            {
                "MM/dd/yyyy h:mm tt",      // "08/12/2025 5:13 PM" (your format)
                "MM/dd/yyyy hh:mm tt",     // "08/12/2025 05:13 PM" 
                "MM/dd/yyyy H:mm",         // "08/12/2025 17:13"
                "MM/dd/yyyy HH:mm",        // "08/12/2025 17:13"
                "MM/dd/yyyy h:mm:ss tt",   // "08/12/2025 5:13:00 PM"
                "MM/dd/yyyy HH:mm:ss",     // "08/12/2025 17:13:00"
                "dd/MM/yyyy h:mm tt",      // "12/08/2025 5:13 PM" (alternate format)
                "dd/MM/yyyy HH:mm",        // "12/08/2025 17:13"
                "yyyy-MM-dd h:mm tt",      // "2025-08-12 5:13 PM"
                "yyyy-MM-dd hh:mm tt",     // "2025-08-12 05:13 PM"
                "yyyy-MM-dd H:mm",         // "2025-08-12 17:13"
                "yyyy-MM-dd HH:mm",        // "2025-08-12 17:13"
                "yyyy-MM-dd h:mm:ss tt",   // "2025-08-12 5:13:00 PM"
                "yyyy-MM-dd HH:mm:ss",     // "2025-08-12 17:13:00"
                "yyyy-MM-ddTHH:mm:ss",     // "2025-08-12T17:13:00"
                "yyyy-MM-dd HH:mm:ss.fff", // "2025-08-12 17:13:00.000"
            };

            foreach (var format in nepalDateTimeFormats)
            {
                if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedDateTime))
                {
                    return Result<DateTime>.Success(DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Unspecified));
                }
            }

            // Fallback to general parsing
            return ParseFlexibleDateTime(input, baseDate);
        }

        /// <summary>
        /// Get Nepal-specific supported formats for user guidance
        /// </summary>
        public static List<string> GetNepalSupportedFormats()
        {
            return new List<string>
            {
                "08/12/2025 5:13 PM",
                "08/12/2025 17:13",
                "2025-08-12 5:13 PM",
                "2025-08-12 17:13:00",
                "12/08/2025 5:13 PM"
            };
        }

    }
   
    }