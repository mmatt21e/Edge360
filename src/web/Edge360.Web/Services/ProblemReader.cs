using System.Net.Http.Json;
using System.Text.Json;

namespace Edge360.Web.Services;

/// <summary>Extracts a human-readable message from an RFC 7807 ProblemDetails response.</summary>
public static class ProblemReader
{
    public static async Task<string> ReadMessageAsync(HttpResponseMessage resp)
    {
        try
        {
            var problem = await resp.Content.ReadFromJsonAsync<JsonElement>();
            if (problem.ValueKind == JsonValueKind.Object)
            {
                // Prefer field-level validation errors, then detail, then title.
                if (problem.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString()))
                        .Where(m => !string.IsNullOrEmpty(m));
                    var joined = string.Join(" ", messages);
                    if (!string.IsNullOrWhiteSpace(joined))
                        return joined;
                }

                if (problem.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                    return detail.GetString()!;
                if (problem.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    return title.GetString()!;
            }
        }
        catch
        {
            // fall through
        }

        return $"Request failed ({(int)resp.StatusCode}).";
    }
}
