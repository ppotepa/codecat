using System.Text.Json;

namespace Codecat.Minification;

public sealed class JsonMinifier : IContentMinifier
{
    public IReadOnlyCollection<string> Languages { get; } = ["json"];

    public bool TryMinify(string content, out string minified)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                document.RootElement.WriteTo(writer);
            }

            minified = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            return true;
        }
        catch (JsonException)
        {
            minified = content;
            return false;
        }
    }
}
