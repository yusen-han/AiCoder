#if WINDOWS
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace AiCoder.Platforms.Windows;

[SupportedOSPlatform("windows10.0.17763.0")]
public static class ScreenshotHelper
{
    public static async Task<string?> CapturePrimaryScreenAsync()
    {
        var path = Path.Combine(FileSystem.CacheDirectory, $"screenshot_{DateTimeOffset.Now:yyyyMMdd_HHmmss}.png");

        await Task.CompletedTask;

        var hdcSrc = GetDC(IntPtr.Zero);
        if (hdcSrc == IntPtr.Zero)
            throw new InvalidOperationException("Failed to get device context.");

        try
        {
            var width = GetDeviceCaps(hdcSrc, DESKTOPHORZRES);
            var height = GetDeviceCaps(hdcSrc, DESKTOPVERTRES);

            var hdcDest = CreateCompatibleDC(hdcSrc);
            var hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
            var hOld = SelectObject(hdcDest, hBitmap);

            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
            SelectObject(hdcDest, hOld);

            DeleteDC(hdcDest);
            ReleaseDC(IntPtr.Zero, hdcSrc);

            using var bitmap = System.Drawing.Image.FromHbitmap(hBitmap);
            DeleteObject(hBitmap);
            bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);

            return path;
        }
        catch
        {
            ReleaseDC(IntPtr.Zero, hdcSrc);
            throw;
        }
    }

    private const int SRCCOPY = 0x00CC0020;
    private const int DESKTOPHORZRES = 118;
    private const int DESKTOPVERTRES = 117;

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);
}
#endif
