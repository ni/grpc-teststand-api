using System;
using System.Runtime.InteropServices;

namespace ExampleClient;

internal static class Win32Interop
{
    // This is defined in RichTextBoxConstants.cs
    // https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/RichTextBoxConstants.cs,425
    private const int MAX_TAB_STOPS = 32;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] // P/INVOKE REVIEWED 2022-09-16
    public struct PARAFORMAT2
    {
        public uint cbSize;
        public uint dwMask;
        public ushort wNumbering;
        public ushort wReserved;
        public int dxStartIndent;
        public int dxRightIndent;
        public int dxOffset;
        public ushort wAlignment;
        public short cTabCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_TAB_STOPS)]
        public int[] rgxTabs;
        public int dySpaceBefore;
        public int dySpaceAfter;
        public int dyLineSpacing;
        public short sStyle;
        public byte bLineSpacingRule;
        public byte bOutlineLevel;
        public ushort wShadingWeight;
        public ushort wNumberingStart;
        public ushort wNumberingStyle;
        public ushort wNumberingTab;
        public ushort wBorderSpace;
        public ushort wBorderWidth;
        public ushort wBorders;
    }

    public enum RichTextBoxOptions : uint
    {
        // EM_SETCHARFORMAT wparam masks

        // Applies the formatting to the current selection. If the selection is empty, the character
        // formatting is applied to the insertion point, and the new character format is in effect
        // only until the insertion point changes.
        SCF_SELECTION = 0x0001,

        // PARAFORMAT 2.0 masks
        PFM_LINESPACING = 0x00000100
    };

    public enum WindowsMessage : int
    {
        WM_USER = 0x0400,
        EM_SETPARAFORMAT = WM_USER + 71
    };

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = false)]
    public static extern IntPtr SendMessage(HandleRef hWnd, uint msg, IntPtr wParam, IntPtr lParam); // P/INVOKE REVIEWED 2022-09-16
}
