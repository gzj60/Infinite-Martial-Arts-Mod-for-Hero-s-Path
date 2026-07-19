using System.Globalization;

namespace ItemSpawner;

public static class QuantityParser
{
    public static bool TryParse(string raw, out int quantity, out string error)
    {
        if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out quantity))
        {
            error = "数量必须是整数。";
            return false;
        }
        if (quantity < 1 || quantity > 999)
        {
            error = "数量范围为 1–999。";
            return false;
        }
        error = string.Empty;
        return true;
    }
}
