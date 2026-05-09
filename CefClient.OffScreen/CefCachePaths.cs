using System.Security.Cryptography;
using System.Text;

namespace CefClient
{
    internal static class CefCachePaths
    {
        public static string RootCachePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "User Data");
        public static string DefaultCachePath => Path.Combine(RootCachePath, "Default");

        public static string GetUvCachePath(string? taskId, string? consumerId, string? uvIndex, string browserId)
        {
            return Path.Combine(
                RootCachePath,
                "TaskSlots",
                SafeSegment(taskId),
                SafeSegment(consumerId),
                SafeSegment(string.IsNullOrWhiteSpace(uvIndex) ? browserId : uvIndex));
        }

        public static string SafeSegment(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            var invalidChars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);

            foreach (var c in value.Trim())
            {
                builder.Append(invalidChars.Contains(c) ? '_' : c);
            }

            var segment = builder.ToString();
            if (segment.Length <= 80)
                return segment;

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(segment))).Substring(0, 12);
            return $"{segment.Substring(0, 67)}_{hash}";
        }
    }
}
