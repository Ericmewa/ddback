using System;

namespace NCBA.DCL.Helpers;

public static class IdResolutionHelper
{
    /// <summary>
    /// Resolves an ID from either _id or Id property
    /// </summary>
    public static Guid? ResolveId(object? obj)
    {
        if (obj == null) return null;

        var type = obj.GetType();

        // Try to get _id property first
        var idProperty = type.GetProperty("_id");
        if (idProperty?.GetValue(obj) is Guid guidId && guidId != Guid.Empty)
            return guidId;

        // Fall back to Id property
        idProperty = type.GetProperty("Id");
        if (idProperty?.GetValue(obj) is Guid id && id != Guid.Empty)
            return id;

        return null;
    }

    /// <summary>
    /// Checks if a document has an ID (either _id or Id)
    /// </summary>
    public static bool HasId(object? obj)
    {
        return ResolveId(obj).HasValue;
    }
}
