using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ItemSpawner;

internal sealed class VirtualizedItemList
{
    private const int PoolSize = 16;
    private const float RowHeight = 42f;
    private readonly ScrollRect scroll;
    private readonly RectTransform content;
    private readonly List<Row> rows = new();
    private List<ItemEntry> items = new();
    private int lastFirstIndex = -1;

    internal ItemEntry Selected { get; private set; }
    internal event Action<ItemEntry> SelectionChanged;

    internal VirtualizedItemList(ScrollRect scroll, RectTransform content, TMP_FontAsset font)
    {
        this.scroll = scroll;
        this.content = content;
        for (int i = 0; i < PoolSize; i++)
        {
            RectTransform rect = UiFactory.Rect($"ItemRow{i}", content);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, RowHeight - 2f);
            Image image = ((Component)rect).gameObject.AddComponent<Image>();
            Button button = ((Component)rect).gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TextMeshProUGUI label = UiFactory.Text(
                rect,
                font,
                string.Empty,
                22f,
                TextAlignmentOptions.MidlineLeft);
            Row row = new(rect, image, button, label);
            button.onClick.AddListener((UnityAction)(() => Select(row)));
            rows.Add(row);
        }
    }

    internal void SetItems(List<ItemEntry> items)
    {
        this.items = items ?? new List<ItemEntry>();
        if (Selected != null && !this.items.Contains(Selected))
        {
            Selected = null;
            SelectionChanged?.Invoke(null);
        }
        float viewportHeight = scroll.viewport == null ? 0f : scroll.viewport.rect.height;
        content.sizeDelta = new Vector2(
            content.sizeDelta.x,
            Mathf.Max(viewportHeight, this.items.Count * RowHeight));
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
        scroll.verticalNormalizedPosition = 1f;
        lastFirstIndex = -1;
        RefreshVisible();
    }

    internal void RefreshVisible()
    {
        int maxFirst = Mathf.Max(0, items.Count - PoolSize);
        int first = Mathf.Clamp(
            Mathf.FloorToInt(content.anchoredPosition.y / RowHeight),
            0,
            maxFirst);
        if (first == lastFirstIndex)
        {
            return;
        }
        lastFirstIndex = first;
        for (int i = 0; i < rows.Count; i++)
        {
            int itemIndex = first + i;
            Row row = rows[i];
            bool active = itemIndex < items.Count;
            ((Component)row.Rect).gameObject.SetActive(active);
            if (!active)
            {
                row.Entry = null;
                continue;
            }
            row.Entry = items[itemIndex];
            row.Rect.anchoredPosition = new Vector2(0f, -itemIndex * RowHeight);
            row.Label.text = $"{row.Entry.IdText}    {row.Entry.Name}";
            row.Image.color = row.Entry == Selected
                ? new Color(0.25f, 0.50f, 0.72f, 0.95f)
                : new Color(0.13f, 0.15f, 0.18f, i % 2 == 0 ? 0.95f : 0.82f);
        }
    }

    private void Select(Row row)
    {
        if (row.Entry == null)
        {
            return;
        }
        Selected = row.Entry;
        lastFirstIndex = -1;
        RefreshVisible();
        SelectionChanged?.Invoke(Selected);
    }

    private sealed class Row
    {
        internal RectTransform Rect { get; }
        internal Image Image { get; }
        internal Button Button { get; }
        internal TextMeshProUGUI Label { get; }
        internal ItemEntry Entry { get; set; }

        internal Row(RectTransform rect, Image image, Button button, TextMeshProUGUI label)
        {
            Rect = rect;
            Image = image;
            Button = button;
            Label = label;
        }
    }
}
