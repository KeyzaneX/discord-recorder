using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;

namespace DiscordVocalOverlay.Services;

public class DragDropService
{
    public void StartFileDropDrag(Window window, string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            var dataObject = new System.Windows.DataObject(System.Windows.DataFormats.FileDrop, new[] { filePath });
            var dragDropEffects = System.Windows.DragDrop.DoDragDrop(window, dataObject, System.Windows.DragDropEffects.Copy);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Drag drop failed: {ex.Message}");
        }
    }
}
