using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ItemSpawner;

internal sealed class ItemSpawnerWindow
{
    private const float WindowWidth = 900f;
    private const float WindowHeight = 650f;
    private const float TitleHeight = 54f;
    private readonly ItemCatalog catalog;
    private readonly ItemGrantService grantService;
    private GameObject root;
    private GameObject ownedEventSystem;
    private RectTransform canvasRect;
    private RectTransform windowRect;
    private RectTransform titleRect;
    private RectTransform closeRect;
    private Action closeAction;
    private bool dragging;
    private Vector2 lastPointer;
    private VirtualizedItemList list;
    private float nextCatalogRetry;

    internal bool IsVisible { get; private set; }
    internal TMP_InputField SearchInput { get; private set; }
    internal TMP_InputField QuantityInput { get; private set; }
    internal ScrollRect ItemScroll { get; private set; }
    internal RectTransform ItemContent { get; private set; }
    internal Button GenerateButton { get; private set; }
    internal TextMeshProUGUI StatusText { get; private set; }

    internal ItemSpawnerWindow(ItemCatalog catalog, ItemGrantService grantService)
    {
        this.catalog = catalog;
        this.grantService = grantService;
    }

    internal void Show(Action closeAction)
    {
        this.closeAction = closeAction;
        if (root == null)
        {
            try
            {
                Build();
            }
            catch
            {
                Dispose();
                throw;
            }
        }
        root.SetActive(true);
        IsVisible = true;
        RefreshSearch();
    }

    internal void Hide()
    {
        if (SearchInput != null)
        {
            SearchInput.DeactivateInputField(false);
        }
        if (QuantityInput != null)
        {
            QuantityInput.DeactivateInputField(false);
        }
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        if (root != null)
        {
            root.SetActive(false);
        }
        IsVisible = false;
        dragging = false;
    }

    internal void Tick()
    {
        if (!IsVisible || root == null)
        {
            return;
        }
        HandleDragging();
        list.RefreshVisible();
        if (!catalog.Loaded && Time.unscaledTime >= nextCatalogRetry)
        {
            nextCatalogRetry = Time.unscaledTime + 1f;
            RefreshSearch();
        }
    }

    internal void Dispose()
    {
        Hide();
        if (root != null)
        {
            Object.Destroy(root);
        }
        if (ownedEventSystem != null)
        {
            Object.Destroy(ownedEventSystem);
        }
        root = null;
        ownedEventSystem = null;
    }

    private void Build()
    {
        EnsureEventSystem();
        TMP_FontAsset font = UiFactory.FindGameFont();
        if (font == null)
        {
            throw new InvalidOperationException("Could not find a TextMeshPro font.");
        }

        root = new GameObject("ItemSpawnerCanvas", Il2CppType.Of<RectTransform>());
        Object.DontDestroyOnLoad(root);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        canvasRect = root.GetComponent<RectTransform>();

        windowRect = UiFactory.Rect("Window", root.transform);
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(WindowWidth, WindowHeight);
        windowRect.anchoredPosition = Vector2.zero;
        Image background = ((Component)windowRect).gameObject.AddComponent<Image>();
        background.color = new Color(0.055f, 0.065f, 0.08f, 0.98f);

        titleRect = UiFactory.Rect("TitleBar", windowRect);
        SetStretchTop(titleRect, TitleHeight, 0f, 0f);
        Image titleBackground = ((Component)titleRect).gameObject.AddComponent<Image>();
        titleBackground.color = new Color(0.12f, 0.15f, 0.19f, 1f);
        UiFactory.Text(titleRect, font, "物品生成器", 28f, TextAlignmentOptions.Center);

        closeRect = UiFactory.Rect("Close", titleRect);
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = Vector2.one;
        closeRect.pivot = new Vector2(1f, 0.5f);
        closeRect.sizeDelta = new Vector2(54f, 0f);
        closeRect.anchoredPosition = Vector2.zero;
        Button closeButton = UiFactory.Button(closeRect, font, "×");
        closeButton.onClick.AddListener((UnityAction)(() => this.closeAction?.Invoke()));

        RectTransform searchRect = UiFactory.Rect("Search", windowRect);
        SetStretchTop(searchRect, 48f, 68f, 20f);
        SearchInput = UiFactory.Input(searchRect, font, "搜索名称或 ID");

        RectTransform viewport = UiFactory.Rect("ItemViewport", windowRect);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(20f, 132f);
        viewport.offsetMax = new Vector2(-20f, -126f);
        ((Component)viewport).gameObject.AddComponent<RectMask2D>();
        ItemScroll = ((Component)viewport).gameObject.AddComponent<ScrollRect>();
        ItemScroll.viewport = viewport;
        ItemScroll.horizontal = false;
        ItemScroll.vertical = true;
        ItemScroll.movementType = ScrollRect.MovementType.Clamped;
        ItemScroll.scrollSensitivity = 34f;
        ItemContent = UiFactory.Rect("Content", viewport);
        ItemContent.anchorMin = new Vector2(0f, 1f);
        ItemContent.anchorMax = new Vector2(1f, 1f);
        ItemContent.pivot = new Vector2(0.5f, 1f);
        ItemContent.sizeDelta = new Vector2(0f, 1f);
        ItemScroll.content = ItemContent;
        list = new VirtualizedItemList(ItemScroll, ItemContent, font);
        SearchInput.onValueChanged.AddListener((UnityAction<string>)(_ => RefreshSearch()));
        list.SelectionChanged += _ => RefreshGenerateState();

        RectTransform quantityRect = UiFactory.Rect("Quantity", windowRect);
        quantityRect.anchorMin = Vector2.zero;
        quantityRect.anchorMax = Vector2.zero;
        quantityRect.pivot = Vector2.zero;
        quantityRect.anchoredPosition = new Vector2(20f, 70f);
        quantityRect.sizeDelta = new Vector2(180f, 44f);
        QuantityInput = UiFactory.Input(quantityRect, font, "数量 1–999");
        QuantityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        QuantityInput.characterLimit = 3;
        QuantityInput.text = "1";
        QuantityInput.onValueChanged.AddListener((UnityAction<string>)(_ => RefreshGenerateState()));

        RectTransform generateRect = UiFactory.Rect("Generate", windowRect);
        generateRect.anchorMin = Vector2.zero;
        generateRect.anchorMax = Vector2.zero;
        generateRect.pivot = Vector2.zero;
        generateRect.anchoredPosition = new Vector2(216f, 70f);
        generateRect.sizeDelta = new Vector2(170f, 44f);
        GenerateButton = UiFactory.Button(generateRect, font, "生成物品");
        GenerateButton.interactable = false;
        GenerateButton.onClick.AddListener((UnityAction)GenerateSelectedItem);

        RectTransform statusRect = UiFactory.Rect("Status", windowRect);
        statusRect.anchorMin = Vector2.zero;
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = Vector2.zero;
        statusRect.anchoredPosition = new Vector2(20f, 32f);
        statusRect.sizeDelta = new Vector2(-40f, 32f);
        StatusText = UiFactory.Text(statusRect, font, string.Empty, 20f, TextAlignmentOptions.MidlineLeft);

        RectTransform warningRect = UiFactory.Rect("Warning", windowRect);
        warningRect.anchorMin = Vector2.zero;
        warningRect.anchorMax = new Vector2(1f, 0f);
        warningRect.pivot = Vector2.zero;
        warningRect.anchoredPosition = new Vector2(20f, 4f);
        warningRect.sizeDelta = new Vector2(-40f, 28f);
        TextMeshProUGUI warning = UiFactory.Text(
            warningRect,
            font,
            "提示：任务或隐藏物品可能影响存档。",
            18f,
            TextAlignmentOptions.MidlineLeft);
        warning.color = new Color(1f, 0.74f, 0.34f, 1f);
        root.SetActive(false);
    }

    private void RefreshSearch()
    {
        if (!catalog.TryLoad(out string error))
        {
            StatusText.text = error;
            list.SetItems(new List<ItemEntry>());
            return;
        }
        List<ItemEntry> results = catalog.Search(SearchInput.text);
        list.SetItems(results);
        StatusText.text = $"找到 {results.Count} 件物品。";
        RefreshGenerateState();
    }

    private void RefreshGenerateState()
    {
        bool quantityValid = QuantityParser.TryParse(QuantityInput.text, out _, out _);
        GenerateButton.interactable = list.Selected != null &&
            quantityValid &&
            grantService.IsReady(out _);
    }

    private void GenerateSelectedItem()
    {
        if (!QuantityParser.TryParse(QuantityInput.text, out int quantity, out string error))
        {
            StatusText.text = error;
            GenerateButton.interactable = false;
            return;
        }
        GrantResult result = grantService.Grant(list.Selected, quantity);
        StatusText.text = result.Message;
        RefreshGenerateState();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }
        ownedEventSystem = new GameObject("ItemSpawnerEventSystem");
        Object.DontDestroyOnLoad(ownedEventSystem);
        ownedEventSystem.AddComponent<EventSystem>();
        ownedEventSystem.AddComponent<StandaloneInputModule>();
    }

    private void HandleDragging()
    {
        Vector2 screenPoint = Input.mousePosition;
        if (Input.GetMouseButtonDown(0) &&
            RectTransformUtility.RectangleContainsScreenPoint(titleRect, screenPoint, null) &&
            !RectTransformUtility.RectangleContainsScreenPoint(closeRect, screenPoint, null) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out lastPointer))
        {
            dragging = true;
        }
        if (dragging && Input.GetMouseButton(0) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 current))
        {
            windowRect.anchoredPosition += current - lastPointer;
            lastPointer = current;
            ClampWindow();
        }
        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }
    }

    private void ClampWindow()
    {
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;
        Vector2 position = windowRect.anchoredPosition;
        position.x = Mathf.Clamp(
            position.x,
            -canvasWidth * 0.5f - WindowWidth * 0.5f + 120f,
            canvasWidth * 0.5f + WindowWidth * 0.5f - 120f);
        position.y = Mathf.Clamp(
            position.y,
            -canvasHeight * 0.5f - WindowHeight * 0.5f + TitleHeight,
            canvasHeight * 0.5f + WindowHeight * 0.5f - TitleHeight);
        windowRect.anchoredPosition = position;
    }

    private static void SetStretchTop(RectTransform rect, float height, float top, float horizontalInset)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(horizontalInset, -top - height);
        rect.offsetMax = new Vector2(-horizontalInset, -top);
    }
}
