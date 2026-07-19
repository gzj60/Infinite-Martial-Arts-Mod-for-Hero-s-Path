using System;

namespace ItemSpawner;

public sealed class ItemEntry
{
    public GameData.ItemData Template { get; }
    public int Id { get; }
    public string IdText { get; }
    public string Name { get; }
    public string InternalName { get; }

    public ItemEntry(GameData.ItemData template, string name, string internalName)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
        Id = template.Uid;
        IdText = Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Name = name;
        InternalName = internalName;
    }

    public bool Matches(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        query = query.Trim();
        return Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            IdText.Contains(query, StringComparison.Ordinal);
    }
}
