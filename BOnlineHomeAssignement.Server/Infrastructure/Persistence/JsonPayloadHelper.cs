using System.Text.Json;
using System.Text.Json.Serialization;

namespace BOnlineHomeAssignement.Server.Infrastructure.Persistence
{
    /// <summary>
    /// Helper utilities for JSON serialization/deserialization of supplier payloads.
    /// Provides consistent JSON handling across the application.
    /// </summary>
    public static class JsonPayloadHelper
    {
        private static readonly JsonSerializerOptions DefaultOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions PrettyOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Serialize an object to JSON string (compact).
        /// </summary>
        public static string Serialize<T>(T obj) where T : class
        {
            return JsonSerializer.Serialize(obj, DefaultOptions);
        }

        /// <summary>
        /// Serialize an object to JSON string (formatted for readability).
        /// </summary>
        public static string SerializePretty<T>(T obj) where T : class
        {
            return JsonSerializer.Serialize(obj, PrettyOptions);
        }

        /// <summary>
        /// Deserialize a JSON string to an object.
        /// Returns null if deserialization fails.
        /// </summary>
        public static T? Deserialize<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(json, DefaultOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Safely merge two JSON payloads, preferring keys from the first object.
        /// Useful for combining supplier data with tenant-specific enrichment.
        /// </summary>
        public static string? MergePayloads(string? payload1, string? payload2)
        {
            if (string.IsNullOrWhiteSpace(payload1) && string.IsNullOrWhiteSpace(payload2))
                return null;

            using var doc1 = payload1 != null ? JsonDocument.Parse(payload1) : null;
            using var doc2 = payload2 != null ? JsonDocument.Parse(payload2) : null;

            var result = new Dictionary<string, object>();

            // Add payload2 first (lower priority)
            if (doc2 != null)
            {
                foreach (var prop in doc2.RootElement.EnumerateObject())
                {
                    result[prop.Name] = prop.Value.Clone();
                }
            }

            // Merge payload1 (higher priority)
            if (doc1 != null)
            {
                foreach (var prop in doc1.RootElement.EnumerateObject())
                {
                    result[prop.Name] = prop.Value.Clone();
                }
            }

            return JsonSerializer.Serialize(result, PrettyOptions);
        }

        /// <summary>
        /// Extract a specific field from a JSON payload.
        /// Example: GetField(payload, "utmSource") returns the UTM source value.
        /// </summary>
        public static T? GetField<T>(string? json, string fieldName) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(fieldName, out var element))
                {
                    return JsonSerializer.Deserialize<T>(element.GetRawText(), DefaultOptions);
                }
            }
            catch (JsonException)
            {
                // Ignore parse errors
            }

            return null;
        }

        /// <summary>
        /// Check if a JSON payload contains a specific field.
        /// </summary>
        public static bool HasField(string? json, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty(fieldName, out _);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Convert an anonymous object to a properly formatted JSON string.
        /// Useful for building payloads dynamically.
        /// </summary>
        public static string ToJson(object obj)
        {
            return Serialize((dynamic)obj);
        }
    }
}
