using System;
using WuLin;

namespace ItemSpawner;

public sealed class ItemGrantService
{
    public bool IsReady(out string reason)
    {
        PlayerTeamManager manager = PlayerTeamManager.Instance;
        if (manager == null || manager.TeamInventory == null)
        {
            reason = "请先进入一个有效存档。";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    public GrantResult Grant(ItemEntry entry, int quantity)
    {
        if (entry == null)
        {
            return new GrantResult(false, "请先选择物品。");
        }
        if (quantity < 1 || quantity > 999)
        {
            return new GrantResult(false, "数量范围为 1–999。");
        }
        if (!IsReady(out string reason))
        {
            return new GrantResult(false, reason);
        }

        try
        {
            bool added = PlayerTeamManager.Instance.TeamInventory.AddItem(entry.Id, quantity, true);
            return added
                ? new GrantResult(true, $"已获得“{entry.Name}”×{quantity}。")
                : new GrantResult(false, "生成失败，物品无效或背包容量不足。");
        }
        catch (Exception ex)
        {
            ItemSpawnerPlugin.Logger?.LogError($"Failed to grant item {entry.Id} x{quantity}: {ex}");
            return new GrantResult(false, "生成物品时发生错误，请查看 BepInEx 日志。");
        }
    }
}
