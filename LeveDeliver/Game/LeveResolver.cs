using Dalamud.Memory;
using LeveDeliver.Data;

namespace LeveDeliver.Game;

/// <summary>
/// Resolves the currently selected leve in the GuildLeve window to a RowId,
/// and maps leve RowIds to delivery NPCs via the embedded database.
/// </summary>
public static unsafe class LeveResolver
{
    /// <summary>
    /// Reads the leve name selected in the GuildLeve window, resolves it against the
    /// embedded database (by name, then by exact RowId). Returns null if unresolvable.
    /// </summary>
    public static LeveEntry? ResolveSelected(LeveDatabase db)
    {
        var addonPtr = Service.GameGui.GetAddonByName("GuildLeve");
        if (addonPtr == nint.Zero)
            return null;

        var guildLeve = (AddonGuildLeve*)addonPtr.Address;
        var numEntries = guildLeve->AtkValues[25].UInt;
        if (numEntries == 0)
            return null;

        // Name of the currently selected entry (AtkValue index 1233 per ChilledLeves GuildLeve.cs).
        var selected = guildLeve->AtkValues[1233].String;
        if (selected.Value == null)
            return null;

        var name = MemoryHelper.ReadSeStringNullTerminated((nint)selected.Value).ToString();
        return ResolveByName(db, name);
    }

    /// <summary>
    /// Resolves an English or client-localized leve display name to its database entry.
    /// </summary>
    public static LeveEntry? ResolveByName(LeveDatabase db, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalized = name.Trim();
        if (db.ByName.TryGetValue(normalized, out var byName))
            return byName;

        var paren = normalized.IndexOfAny(['(', '（']);
        var shortName = paren > 0 ? normalized[..paren].Trim() : normalized;
        if (paren > 0 && db.ByName.TryGetValue(shortName, out byName))
            return byName;

        foreach (var row in Service.Data.GetExcelSheet<Lumina.Excel.Sheets.Leve>())
        {
            var rowName = row.Name.ToString();
            if (string.Equals(rowName, normalized, StringComparison.OrdinalIgnoreCase)
                || string.Equals(rowName, shortName, StringComparison.OrdinalIgnoreCase))
                return db.ById.GetValueOrDefault(row.RowId);
        }

        return null;
    }

    public static bool Matches(LeveDatabase db, string? name, LeveEntry entry)
        => ResolveByName(db, name)?.Id == entry.Id;
}
