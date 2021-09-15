using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CheckBadThingsCSharp.lib {
    class VerifyFile {
        private const int WTD_UI_NONE = 2;
        private const int WTD_REVOKE_NONE = 0;
        private const int WTD_CHOICE_FILE = 1;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");

        [DllImport("wintrust.dll")]
        private static extern int WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID, ref WINTRUST_DATA pWVTData);
        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct WINTRUST_DATA {
            public int cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public int dwUIChoice;
            public int fdwRevocationChecks;
            public int dwUnionChoice;
            public IntPtr pFile;
            public int dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public int dwProvFlags;
            public int dwUIContext;
            public IntPtr pSignatureSettings;
        }

        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_FILE_INFO {
            public int cbStruct;
            public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }
        public static bool IsSigned(string filePath) {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            var file = new WINTRUST_FILE_INFO();
            file.cbStruct = Marshal.SizeOf(typeof(WINTRUST_FILE_INFO));
            file.pcwszFilePath = filePath;

            var data = new WINTRUST_DATA();
            data.cbStruct = Marshal.SizeOf(typeof(WINTRUST_DATA));
            data.dwUIChoice = WTD_UI_NONE;
            data.dwUnionChoice = WTD_CHOICE_FILE;
            data.fdwRevocationChecks = WTD_REVOKE_NONE;
            data.pFile = Marshal.AllocHGlobal(file.cbStruct);
            Marshal.StructureToPtr(file, data.pFile, false);

            int hr;
            try {
                hr = WinVerifyTrust(INVALID_HANDLE_VALUE, WINTRUST_ACTION_GENERIC_VERIFY_V2, ref data);
            } finally {
                Marshal.FreeHGlobal(data.pFile);
            }
            return hr == 0;
        }
    }
}
