using System;
using System.Collections.Generic;
using GameData;
using WuLin;

namespace ItemSpawner;

public sealed class ItemCatalog
{
    private readonly List<ItemEntry> entries = new();

    public bool Loaded { get; private set; }
    public IReadOnlyList<ItemEntry> Entries => entries;

    public bool TryLoad(out string error)
    {
        error = string.Empty;
        if (Loaded)
        {
            return true;
        }

        ItemDataScriptObject table;
        try
        {
            table = BaseDataClass.GetGameData<ItemDataScriptObject>();
        }
        catch (Exception)
        {
            error = "读取游戏物品数据失败。";
            return false;
        }

        if (table == null || table.ItemData == null || table.ItemData.Length == 0)
        {
            error = "游戏物品数据尚未就绪。";
            return false;
        }

        entries.Clear();
        for (int i = 0; i < table.ItemData.Length; i++)
        {
            ItemData item = table.ItemData[i];
            if (item == null || item.Uid <= 0)
            {
                continue;
            }

            string internalName = item.UName ?? string.Empty;
            string displayName;
            try
            {
                displayName = GameUtil.GetName(item, false);
            }
            catch (Exception)
            {
                displayName = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = string.IsNullOrWhiteSpace(internalName) ? "未命名物品" : internalName;
            }

            entries.Add(new ItemEntry(item, displayName, internalName));
        }

        entries.Sort((left, right) => left.Id.CompareTo(right.Id));
        Loaded = entries.Count > 0;
        if (!Loaded)
        {
            error = "物品表中没有有效记录。";
        }
        return Loaded;
    }

    public List<ItemEntry> Search(string query)
    {
        List<ItemEntry> results = new();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Matches(query))
            {
                results.Add(entries[i]);
            }
        }
        return results;
    }
}
