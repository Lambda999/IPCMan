using System.Security.Cryptography;
using System.Text;

namespace CefClient
{
    internal static class CefCachePaths
    {
        public static string RootCachePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "User Data");

        public static string GetTaskSlotCachePath(string? taskId, string browserId)
        {
            return Path.Combine(RootCachePath, "TaskSlots", SafeSegment(taskId), SafeSegment(browserId));
        }

        public static string GetLegacyCachePath(string cacheIndex)
        {
            var segments = cacheIndex.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(SafeSegment)
                .ToArray();

            return Path.Combine(new[] { RootCachePath }.Concat(segments).ToArray());
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
