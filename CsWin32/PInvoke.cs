using Microsoft.Windows.Sdk;
using System;
using System.Linq;

namespace CsWin32
{
    public static class Kernel32
    {
        private static readonly int BackgroundColor = (int)Console.BackgroundColor;
        private static readonly int ForegroundColor = (int)Console.ForegroundColor;
        private static readonly ushort Attributes = (ushort)(ForegroundColor | (BackgroundColor << 4));

        public static bool WriteConsoleOutput(Handle handle, string text, int x, int y, int width, int height)
        {
            // Pad any extra space we have
            var fill = $"{text}{new string(' ', width * height - text.Length)}";

            static CHAR_INFO ToCharInfo(char c)
            {
                var info = new CHAR_INFO();
                // Give it our character to write
                info.Char.UnicodeChar = c;
                // Use our attributes
                info.Attributes = Attributes;
                // Return info for this character
                return info;
            }

            var buf = fill.Select(ToCharInfo).ToArray();

            // Make a buffer size out our dimensions
            var dwBufferSize = new COORD
            {
                X = (short)width,
                Y = (short)height
            };

            // Not really sure what this is but its probably important
            var dwBufferCoord = new COORD
            {
                X = 0,
                Y = 0
            };

            // Where do we place this?
            var lpWriteRegion = new SMALL_RECT { 
                Left = (short)x, 
                Top = (short)y, 
                Right = (short)(x + width), 
                Bottom = (short)(y + height) 
            };

            unsafe
            {
                fixed (CHAR_INFO* lpBuffer = &buf[0])
                {
                    var result = PInvoke.WriteConsoleOutput(handle.Value, lpBuffer, dwBufferSize, dwBufferCoord, ref lpWriteRegion);
                    return result.Value;
                }
            }
        }

        public static void ClearScreen(Handle handle)
        {
            var screenBufferInfoSuccess = PInvoke.GetConsoleScreenBufferInfo(handle.Value, out var lpConsoleScreenBufferInfo);
            if(!screenBufferInfoSuccess.Value)
            {
                return;
            }

            // Scroll the rectangle of the entire buffer.
            var scrollRect = new SMALL_RECT
            {
                Left = 0,
                Top = 0,
                Right = 0,
                Bottom = 0
            };

            // Scroll it upwards off the top of the buffer with a magnitude of the entire height.
            var scrollTarget = new COORD
            {
                X = 0,
                Y = (short)(0 - lpConsoleScreenBufferInfo.dwSize.Y)
            };

            // Fill with empty spaces with the buffer's default text attribute.
            var fill = new CHAR_INFO();
            fill.Char.UnicodeChar = ' ';
            fill.Attributes = lpConsoleScreenBufferInfo.wAttributes;

            // Do the scroll
            PInvoke.ScrollConsoleScreenBuffer(handle.Value, scrollRect, null, scrollTarget, fill);

            // Move the cursor to the top left corner too.
            lpConsoleScreenBufferInfo.dwCursorPosition.X = 0;
            lpConsoleScreenBufferInfo.dwCursorPosition.Y = 0;

            PInvoke.SetConsoleCursorPosition(handle.Value, lpConsoleScreenBufferInfo.dwCursorPosition);
        }

        public static Handle GetStdHandle()
        {
            return new Handle(PInvoke.GetStdHandle(STD_HANDLE_TYPE.STD_OUTPUT_HANDLE));
        }

        public static bool EnableVTMode(Handle handle)
        {
            var getConsoleModeSuccess = PInvoke.GetConsoleMode(handle.Value, out var lpMode);
            if (!getConsoleModeSuccess.Value)
            {
                return false;
            }

            //https://docs.microsoft.com/en-us/windows/console/setconsolemode
            var setConsoleModeSuccess = PInvoke.SetConsoleMode(handle.Value, lpMode | 0x0004);
            if (!setConsoleModeSuccess.Value)
            {
                return false;
            }
            return true;
        }

        public static bool SetConsoleActiveScreenBuffer(Handle handle)
        {
            var success = PInvoke.SetConsoleActiveScreenBuffer(handle.Value);
            return success.Value;
        }

        // https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/ConsoleDrivers/WindowsDriver.cs#L70
        [Flags]
        enum DesiredAccess : uint
        {
            GenericRead = 2147483648,
            GenericWrite = 1073741824,
        }

        [Flags]
        enum ShareMode : uint
        {
            FileShareRead = 1,
            FileShareWrite = 2,
        }

        public static Handle CreateConsoleScreenBuffer()
        {
            // INVALID_HANDLE_VALUE = new IntPtr(-1);
            var GenericRead = (uint)DesiredAccess.GenericRead;
            var GenericWrite = (uint)DesiredAccess.GenericWrite;
            var dwDesiredAccess = GenericRead | GenericWrite;
            var FileShareRead = (uint)ShareMode.FileShareRead;
            var FileShareWrite = (uint)ShareMode.FileShareWrite;
            var dwShareMode = FileShareRead | FileShareWrite;
            var lpSecurityAttributes = new SECURITY_ATTRIBUTES?();

            unsafe
            {
                return new Handle(PInvoke.CreateConsoleScreenBuffer(dwDesiredAccess, dwShareMode, lpSecurityAttributes, 1, null));
            }
        }

        public static void SetCursorInvisible(Handle handle)
        {
            PInvoke.GetConsoleCursorInfo(handle.Value, out var lpConsoleCursorInfo);
            lpConsoleCursorInfo.bVisible = false; // set the cursor visibility
            PInvoke.SetConsoleCursorInfo(handle.Value, lpConsoleCursorInfo);
        }
    }

    public record Handle
    {
        internal readonly CloseHandleSafeHandle Value;
        internal Handle(CloseHandleSafeHandle value) => Value = value;
    }
}
