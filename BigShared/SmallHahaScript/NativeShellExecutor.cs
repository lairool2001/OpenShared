using System;
using System.Diagnostics;
using System.Runtime.InteropServices; // 必須引用：用於 DllImport

public class NativeShellExecutor
{
    // 1. 宣告 Windows API (ShellExecute)
    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr ShellExecute(
        IntPtr hwnd,        // 視窗控制代碼 (通常設為 IntPtr.Zero)
        string lpOperation, // 操作動作："open", "print", "runas" (系統管理員)
        string lpFile,      // 要執行的檔案路徑
        string lpParameters,// 參數 (如果有的話)
        string lpDirectory, // 工作目錄 (通常設為 null，使用預設)
        int nShowCmd        // 視窗顯示狀態 (1 = 正常顯示, 0 = 隱藏)
    );

    // 定義視窗顯示常數
    private const int SW_SHOWNORMAL = 1;

    // 2. 包裝成一個簡單的方法
    public static void Execute(string path)
    {
        string args = $"\"{path}\"";

        // 使用 explorer.exe 來開啟檔案
        Process.Start("explorer.exe", args);
    }
}

// === 使用範例 ===
// public static void Main()
// {
//     string batPath = @"D:\YourScript.bat";
//     NativeShellExecutor.Execute(batPath);
// }