using System;
using UnityEngine;

namespace ItemSpawner;

public sealed class ItemSpawnerRuntime : MonoBehaviour
{
    private ItemSpawnerWindow window;
    private bool cursorCaptured;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    public ItemSpawnerRuntime(IntPtr pointer) : base(pointer)
    {
    }

    public void Awake()
    {
        window = new ItemSpawnerWindow(new ItemCatalog(), new ItemGrantService());
    }

    public void Update()
    {
        if (Input.GetKeyDown(ItemSpawnerPlugin.ToggleKey.Value))
        {
            ToggleWindow();
        }
        if (window.IsVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseWindow();
        }
        if (window.IsVisible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            window.Tick();
        }
    }

    internal void ToggleWindow()
    {
        if (window.IsVisible)
        {
            CloseWindow();
        }
        else
        {
            OpenWindow();
        }
    }

    private void OpenWindow()
    {
        if (!cursorCaptured)
        {
            previousCursorVisible = Cursor.visible;
            previousCursorLock = Cursor.lockState;
            cursorCaptured = true;
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        try
        {
            window.Show(CloseWindow);
        }
        catch (Exception ex)
        {
            ItemSpawnerPlugin.Logger.LogError($"Failed to open ItemSpawner: {ex}");
            RestoreCursor();
        }
    }

    private void CloseWindow()
    {
        window.Hide();
        RestoreCursor();
    }

    private void RestoreCursor()
    {
        if (!cursorCaptured)
        {
            return;
        }
        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLock;
        cursorCaptured = false;
    }

    public void OnDestroy()
    {
        window?.Dispose();
        RestoreCursor();
    }
}
