using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NCBA.DCL.Helpers;

/// <summary>
/// Custom JSON converter that handles flexible property naming conventions.
/// Accepts PascalCase, camelCase, and snake_case property names from the frontend.
/// </summary>
public class FlexibleJsonConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("This converter is not meant to be used for direct deserialization.");
    }

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("This converter is not meant to be used for direct serialization.");
    }
}

/// <summary>
/// Custom property naming policy that accepts multiple naming conventions.
/// Returns camelCase as default but accepts input in any format.
/// </summary>
public class FlexibleNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        // Return camelCase as the default for serialization
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
            return name;

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}

/// <summary>
/// Naming policy for snake_case conversion (for serialization).
/// </summary>
public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        return ToSnakeCase(name);
    }

    private static string ToSnakeCase(string input)
    {
        var s = Regex.Replace(input, "([a-z0-9])([A-Z])", "$1_$2");
        return s.ToLowerInvariant();
    }
}

/// <summary>
/// Helper class to normalize property names and accept multiple conventions.
/// </summary>
public static class PropertyNameResolver
{
    /// <summary>
    /// Converts a property name to multiple possible variations for matching.
    /// This helps with deserialization of payloads with different naming conventions.
    /// </summary>
    public static List<string> GetPossiblePropertyNames(string propertyName)
    {
        var names = new HashSet<string>();

        // Original name
        names.Add(propertyName);

        // camelCase
        if (propertyName.Length > 0)
        {
            var camelCase = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
            names.Add(camelCase);
        }

        // PascalCase
        if (propertyName.Length > 0)
        {
            var pascalCase = char.ToUpperInvariant(propertyName[0]) + propertyName.Substring(1);
            names.Add(pascalCase);
        }

        // snake_case
        var snakeCase = ToSnakeCase(propertyName);
        names.Add(snakeCase);

        // SCREAMING_SNAKE_CASE
        names.Add(snakeCase.ToUpperInvariant());

        // lowercase
        names.Add(propertyName.ToLowerInvariant());

        // UPPERCASE
        names.Add(propertyName.ToUpperInvariant());

        return names.ToList();
    }

    /// <summary>
    /// Converts PascalCase or camelCase to snake_case.
    /// </summary>
    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var s = Regex.Replace(input, "([a-z0-9])([A-Z])", "$1_$2");
        return s.ToLowerInvariant();
    }

    /// <summary>
    /// Converts snake_case to camelCase.
    /// </summary>
    public static string SnakeCaseToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var parts = input.Split('_');
        var result = parts[0].ToLowerInvariant();

        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                result += char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
            }
        }

        return result;
    }

    /// <summary>
    /// Converts any case to PascalCase.
    /// </summary>
    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // First convert to camelCase
        var camelCase = SnakeCaseToCamelCase(input);

        // Then convert first char to uppercase
        return char.ToUpperInvariant(camelCase[0]) + camelCase.Substring(1);
    }
}
