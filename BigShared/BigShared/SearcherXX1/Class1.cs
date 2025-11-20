using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class RichTextBoxHelper
{
    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    // 定義 SCROLLINFO 結構
    [StructLayout(LayoutKind.Sequential)]
    public struct SCROLLINFO
    {
        public uint cbSize;
        public uint fMask;
        public int nMin;
        public int nMax;
        public uint nPage;
        public int nPos;       // 滾動偏移
        public int nTrackPos;  // 拖動時偏移
    }    // 常數
    private const int WM_VSCROLL = 0x115;
    private const int SB_THUMBPOSITION = 4;
    private const int SB_THUMBTRACK = 5;

    // API 函數
    [DllImport("user32.dll")]
    static extern bool GetScrollInfo(IntPtr hwnd, int bar, ref SCROLLINFO scrollInfo);

    // 滾動到特定偏移行數（例如：第n行）
    public static void ScrollToLine(RichTextBox rtb, int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= rtb.Lines.Length)
            return;
        // 每行高度假設為固定值
        // 更為精確的方法是使用 GetPositionFromCharIndex
        int charIndex = rtb.GetFirstCharIndexFromLine(lineIndex);
        rtb.SelectionStart = charIndex;
        rtb.ScrollToCaret(); // 使游標所在位置置底
    }
    /// <summary>
    /// 滾動到指定進度（0.0 到 1.0）
    /// </summary>
    public static void ScrollToProgress(RichTextBox rtb, double progress) // progress: 0.0 ~ 1.0
    {
        if (progress < 0.0 || progress > 1.0)
            throw new ArgumentOutOfRangeException(nameof(progress), "Must be between 0.0 and 1.0");
        // 取得滾動資訊
        SCROLLINFO si = new SCROLLINFO();
        si.cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO));
        si.fMask = 0x10; // SIF_POS | SIF_RANGE | SIF_PAGE
        if (GetScrollInfo(rtb.Handle, 1 /* SB_VERT */, ref si))
        {
            int maxScroll = si.nMax - (int)si.nPage + 1; // 滾動範圍
            int targetPos = (int)(progress * maxScroll);
            // 設定滾動位置
            SendMessage(rtb.Handle, WM_VSCROLL, new IntPtr(SB_THUMBPOSITION), new IntPtr(targetPos));
        }
    }
    // 讀取垂直滾動位置
    public static int GetCurrentScrollPosition(RichTextBox rtb)
    {
        SCROLLINFO si = new SCROLLINFO();
        si.cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO));
        si.fMask = 0x10; // SIF_POS | SIF_RANGE | SIF_PAGE
        if (GetScrollInfo(rtb.Handle, 1, ref si))
            return si.nPos;
        return 0;
    }
    // 設定滾動位置
    public static void SetScrollPosition(RichTextBox rtb, int position)
    {
        SendMessage(rtb.Handle, WM_VSCROLL, new IntPtr(SB_THUMBPOSITION), new IntPtr(position));
    }
}
