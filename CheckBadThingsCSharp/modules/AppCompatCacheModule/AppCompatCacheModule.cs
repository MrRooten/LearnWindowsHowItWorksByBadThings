using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

//Code mainly is from https://github.com/EricZimmerman/AppCompatCacheParser
//Thinks for his coding and research
namespace CheckBadThingsCSharp.modules {
    class AppCompatCacheModule : IModule {
        public enum Execute {
            Yes,
            No,
            NA
        }

        [Flags]
        public enum InsertFlag {
            Unknown1 = 0x00000001,
            Executed = 0x00000002,
            Unknown4 = 0x00000004,
            Unknown8 = 0x00000008,
            Unknown10 = 0x00000010,
            Unknown20 = 0x00000020,
            Unknown40 = 0x00000040,
            Unknown80 = 0x00000080,
            Unknown10000 = 0x00010000,
            Unknown20000 = 0x00020000,
            Unknown30000 = 0x00030000,
            Unknown40000 = 0x00040000,
            Unknown100000 = 0x00100000,
            Unknown200000 = 0x00200000,
            Unknown400000 = 0x00400000,
            Unknown800000 = 0x00800000
        }
        public enum OperatingSystemVersion {
            WindowsXP,
            WindowsVistaWin2k3Win2k8,
            Windows7x86,
            Windows7x64_Windows2008R2,
            Windows80_Windows2012,
            Windows81_Windows2012R2,
            Windows10,
            Windows10Creators,
            Unknown
        }

        private byte[] readBytes() {
            var keyCurrUser = Microsoft.Win32.Registry.LocalMachine;
            var subKey2 = keyCurrUser.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\AppCompatCache");
            var rawBytes = (byte[])subKey2.GetValue("AppCompatCache", null);
            return rawBytes;
        }
        public class CacheEntry {
            public int CacheEntryPosition { get; set; }
            public int CacheEntrySize { get; set; }
            public byte[] Data { get; set; }
            public InsertFlag InsertFlags { get; set; }
            public Execute Executed { get; set; }
            public int DataSize { get; set; }
            public DateTimeOffset? LastModifiedTimeUTC { get; set; }
            public long LastModifiedFILETIMEUTC => LastModifiedTimeUTC.HasValue ? LastModifiedTimeUTC.Value.ToFileTime() : 0;
            public string Path { get; set; }
            public int PathSize { get; set; }
            public string Signature { get; set; }
            public int ControlSet { get; set; }

            public override string ToString() {
                return
                    $"#{CacheEntryPosition} (Path size: {PathSize}), Path: {Path}, Last modified (UTC):{LastModifiedTimeUTC}";
            }

            public bool Duplicate { get; set; }
            public string SourceFile { get; set; }

            public string GetKey() {
                return $"{this.LastModifiedFILETIMEUTC}{this.Path.ToUpperInvariant()}";
            }
        }
        public interface IAppCompatCache {
            List<CacheEntry> Entries { get; }

            /// <summary>
            ///     The total number of entries to expect
            /// </summary>
            /// <remarks>When not available (Windows 8.x/10), will return -1</remarks>
            int EntryCount { get; }

            int ControlSet { get; }
        }
        public class WindowsXP : IAppCompatCache {
            public WindowsXP(byte[] rawBytes, bool is32Bit, int controlSet) {
                Entries = new List<CacheEntry>();

                var index = 4;
                ControlSet = controlSet;

                EntryCount = BitConverter.ToInt32(rawBytes, index);
                index += 4;

                var lruArrauEntries = BitConverter.ToUInt32(rawBytes, index);
                index += 4;

                index = 400;

                var position = 0;

                if (EntryCount == 0) {
                    return;
                }

                if (is32Bit) {
                    while (index < rawBytes.Length) {
                        try {

                            var ce = new CacheEntry { PathSize = 528 };


                            ce.Path = Encoding.Unicode.GetString(rawBytes, index, ce.PathSize).Split('\0').First().Replace('\0', ' ').Trim().Replace(@"\??\", "");
                            index += 528;

                            ce.LastModifiedTimeUTC =
                                DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();
                            index += 8;

                            var fileSize = BitConverter.ToUInt64(rawBytes, index);
                            index += 8;

                            //                        ce.LastModifiedTimeUTC =
                            //                            DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();
                            //this is last update time, its not reported yet
                            index += 8;

                            //                        if (ce.LastModifiedTimeUTC.HasValue == false)
                            //                        {
                            //                            break;
                            //                        }

                            ce.CacheEntryPosition = position;
                            ce.ControlSet = controlSet;

                            ce.Executed = AppCompatCacheModule.Execute.NA;


                            Entries.Add(ce);
                            position += 1;

                            if (Entries.Count == EntryCount) {
                                break;
                            }
                        } catch (Exception ex) {
                           
                            //TODO Report this
                            if (Entries.Count < EntryCount) {
                                throw;
                            }
                            //take what we can get
                            break;
                        }
                    }
                } else {
                    throw new Exception(
                        "64 bit XP support not available. send the hive to saericzimmerman@gmail.com so support can be added");
                    //                while (index < rawBytes.Length)
                    //                {
                    //                    try
                    //                    {
                    //                        var ce = new CacheEntry {PathSize = BitConverter.ToUInt16(rawBytes, index)};
                    //
                    //                        index += 2;
                    //
                    //                        var maxPathSize = BitConverter.ToUInt16(rawBytes, index);
                    //                        index += 2;
                    //
                    //
                    //                        var pathOffset = BitConverter.ToInt32(rawBytes, index);
                    //                        index += 4;
                    //
                    //                        ce.LastModifiedTimeUTC =
                    //                            DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();
                    //                        index += 8;
                    //
                    //                        // skip 4 unknown (insertion flags?)
                    //                        index += 4;
                    //
                    //                        // skip 4 unknown (shim flags?)
                    //                        index += 4;
                    //
                    //                        var ceDataSize = BitConverter.ToUInt32(rawBytes, index);
                    //                        index += 4;
                    //
                    //                        var dataOffset = BitConverter.ToUInt32(rawBytes, index);
                    //                        index += 4;
                    //
                    //                        ce.Path = Encoding.Unicode.GetString(rawBytes, pathOffset, ce.PathSize);
                    //
                    //                        if (ce.LastModifiedTimeUTC.Year == 1601)
                    //                        {
                    //                            break;
                    //                        }
                    //
                    //                        ce.CacheEntryPosition = position;
                    //                        Entries.Add(ce);
                    //                        position += 1;
                    //
                    //
                    //                        if (Entries.Count == EntryCount)
                    //                        {
                    //                            break;
                    //                        }
                    //                    }
                    //                    catch (Exception ex)
                    //                    {
                    //                        //TODO Report this
                    //                        Debug.WriteLine(ex.Message);
                    //                        //take what we can get
                    //                        break;
                    //                    }
                    //                }
                }
            }

            public List<CacheEntry> Entries { get; }
            public int EntryCount { get; }
            public int ControlSet { get; }
        }
    

    public class VistaWin2k3Win2k8 : IAppCompatCache {
            public VistaWin2k3Win2k8(byte[] rawBytes, bool is32Bit, int controlSet) {
                Entries = new List<CacheEntry>();

                var index = 4;
                ControlSet = controlSet;

                EntryCount = BitConverter.ToInt32(rawBytes, index);

                index = 8;

                var position = 0;

                if (EntryCount == 0) {
                    return; ;
                }

                if (is32Bit) {
                    while (index < rawBytes.Length) {
                        try {
                            var ce = new CacheEntry();

                            ce.PathSize = BitConverter.ToUInt16(rawBytes, index);
                            index += 2;

                            var maxPathSize = BitConverter.ToUInt16(rawBytes, index);
                            index += 2;


                            var pathOffset = BitConverter.ToInt32(rawBytes, index);
                            index += 4;

                            ce.LastModifiedTimeUTC =
                                DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();

                            if (ce.LastModifiedTimeUTC.Value.Year == 1601) {
                                ce.LastModifiedTimeUTC = null;
                            }

                            index += 8;

                            // skip 4 unknown (insertion flags?)
                            ce.InsertFlags = (AppCompatCacheModule.InsertFlag)BitConverter.ToInt32(rawBytes, index);
                            index += 4;

                            // skip 4 unknown (shim flags?)
                            index += 4;

                            ce.Path = Encoding.Unicode.GetString(rawBytes, pathOffset, ce.PathSize).Replace(@"\??\", "");

                            //                        if ((ce.InsertFlags & AppCompatCache.InsertFlag.Executed) == AppCompatCache.InsertFlag.Executed)
                            //                        {
                            //                            ce.Executed = AppCompatCache.Execute.Yes;
                            //                        }
                            //                        else
                            //                        {
                            //                            ce.Executed = AppCompatCache.Execute.No;
                            //                        }

                            ce.Executed = AppCompatCacheModule.Execute.NA;

                            ce.CacheEntryPosition = position;
                            ce.ControlSet = controlSet;
                            Entries.Add(ce);
                            position += 1;

                            if (Entries.Count == EntryCount) {
                                break;
                            }
                        } catch (Exception ex) {
                            if (Entries.Count < EntryCount) {
                                throw;
                            }

                            //take what we can get
                            break;
                        }
                    }
                } else {

                    while (index < rawBytes.Length) {
                        try {
                            var ce1 = new CacheEntry();

                            ce1.PathSize = BitConverter.ToUInt16(rawBytes, index);
                            index += 2;

                            var maxPathSize = BitConverter.ToUInt16(rawBytes, index);
                            index += 2;

                            // skip 4 unknown (padding)
                            index += 4;

                            var pathOffset = BitConverter.ToInt64(rawBytes, index);
                            index += 8;

                            ce1.LastModifiedTimeUTC =
                                DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();
                            index += 8;

                            // skip 4 unknown (insertion flags?)
                            ce1.InsertFlags = (AppCompatCacheModule.InsertFlag)BitConverter.ToInt32(rawBytes, index);
                            index += 4;

                            // skip 4 unknown (shim flags?)
                            index += 4;

                            //                        var ceDataSize = BitConverter.ToUInt64(rawBytes, index);
                            //                        index += 8;
                            //
                            //                        var dataOffset = BitConverter.ToUInt64(rawBytes, index);
                            //                        index += 8;

                            ce1.Path = Encoding.Unicode.GetString(rawBytes, (int)pathOffset, ce1.PathSize).Replace(@"\??\", "");

                            if ((ce1.InsertFlags & AppCompatCacheModule.InsertFlag.Executed) == AppCompatCacheModule.InsertFlag.Executed) {
                                ce1.Executed = AppCompatCacheModule.Execute.Yes;
                            } else {
                                ce1.Executed = AppCompatCacheModule.Execute.No;
                            }

                            ce1.CacheEntryPosition = position;
                            ce1.ControlSet = controlSet;
                            Entries.Add(ce1);
                            position += 1;

                            if (Entries.Count == EntryCount) {
                                break;
                            }
                        } catch (Exception ex) {

                            if (Entries.Count < EntryCount) {
                                throw;
                            }
                            //take what we can get
                            break;
                        }
                    }
                }
            }

            public List<CacheEntry> Entries { get; }
            public int EntryCount { get; }
            public int ControlSet { get; }
        }
        public class Windows7 : IAppCompatCache {
            public Windows7(byte[] rawBytes, bool is32Bit, int controlSet) {
                Entries = new List<CacheEntry>();

                var index = 4;
                ControlSet = controlSet;

                EntryCount = BitConverter.ToInt32(rawBytes, index);

                index = 128;

                var position = 0;

                if (EntryCount == 0) {
                    return; ;
                }

                if (is32Bit) {
                    while (index < rawBytes.Length) {
                        try {
                            var ce = new CacheEntry();

                            ce.PathSize = BitConverter.ToUInt16(rawBytes, index);
                            index += 2;

                            var maxPathSize = BitConverter.ToUInt16(rawBytes, index);
                            index += 2;


                            var pathOffset = BitConverter.ToInt32(rawBytes, index);
                            index += 4;

                            ce.LastModifiedTimeUTC =
                                DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();

                            if (ce.LastModifiedTimeUTC.Value.Year == 1601) {
                                ce.LastModifiedTimeUTC = null;
                            }
                            index += 8;

                            // skip 4 unknown (insertion flags?)
                            ce.InsertFlags = (AppCompatCacheModule.InsertFlag)BitConverter.ToInt32(rawBytes, index);
                            index += 4;

                            // skip 4 unknown (shim flags?)
                            index += 4;

                            var ceDataSize = BitConverter.ToUInt32(rawBytes, index);
                            index += 4;

                            var dataOffset = BitConverter.ToUInt32(rawBytes, index);
                            index += 4;

                            ce.Path = Encoding.Unicode.GetString(rawBytes, pathOffset, ce.PathSize).Replace(@"\??\", "");

                            if ((ce.InsertFlags & AppCompatCacheModule.InsertFlag.Executed) == AppCompatCacheModule.InsertFlag.Executed) {
                                ce.Executed = AppCompatCacheModule.Execute.Yes;
                            } else {
                                ce.Executed = AppCompatCacheModule.Execute.No;
                            }

                            ce.CacheEntryPosition = position;
                            ce.ControlSet = controlSet;
                            Entries.Add(ce);
                            position += 1;

                            if (Entries.Count == EntryCount) {
                                break;
                            }
                        } catch (Exception ex) {

                            if (Entries.Count < EntryCount) {
                                throw;
                            }

                            //take what we can get
                            break;
                        }
                    }
                } else {
                    while (index < rawBytes.Length) {
                        try {
                            var ce1 = new CacheEntry();

                            ce1.PathSize = BitConverter.ToUInt16(rawBytes, index);
                            index += 2;

                            var maxPathSize = BitConverter.ToUInt16(rawBytes, index);
                            index += 2;

                            // skip 4 unknown (padding)
                            index += 4;

                            var pathOffset = BitConverter.ToInt64(rawBytes, index);
                            index += 8;

                            ce1.LastModifiedTimeUTC =
                                DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();
                            index += 8;

                            // skip 4 unknown (insertion flags?)
                            ce1.InsertFlags = (AppCompatCacheModule.InsertFlag)BitConverter.ToInt32(rawBytes, index);
                            index += 4;

                            // skip 4 unknown (shim flags?)
                            index += 4;

                            var ceDataSize = BitConverter.ToUInt64(rawBytes, index);
                            index += 8;

                            var dataOffset = BitConverter.ToUInt64(rawBytes, index);
                            index += 8;

                            ce1.Path = Encoding.Unicode.GetString(rawBytes, (int)pathOffset, ce1.PathSize).Replace(@"\??\", "");

                            if ((ce1.InsertFlags & AppCompatCacheModule.InsertFlag.Executed) == AppCompatCacheModule.InsertFlag.Executed) {
                                ce1.Executed = AppCompatCacheModule.Execute.Yes;
                            } else {
                                ce1.Executed = AppCompatCacheModule.Execute.No;
                            }

                            ce1.CacheEntryPosition = position;
                            ce1.ControlSet = controlSet;
                            Entries.Add(ce1);
                            position += 1;

                            if (Entries.Count == EntryCount) {
                                break;
                            }
                        } catch (Exception ex) {

                            //TODO Report this
                            if (Entries.Count < EntryCount) {
                                throw;
                            }
                            //take what we can get
                            break;
                        }
                    }
                }
            }

            public List<CacheEntry> Entries { get; }
            public int EntryCount { get; }
            public int ControlSet { get; }
        }

        public class Windows8x : IAppCompatCache {
            public Windows8x(byte[] rawBytes, AppCompatCacheModule.OperatingSystemVersion os, int controlSet) {
                Entries = new List<CacheEntry>();

                var index = 128;

                var signature = "00ts";

                ControlSet = controlSet;

                EntryCount = -1;

                if (os == AppCompatCacheModule.OperatingSystemVersion.Windows81_Windows2012R2) {
                    signature = "10ts";
                }

                var position = 0;

                while (index < rawBytes.Length) {
                    try {
                        var ce = new CacheEntry {
                            Signature = Encoding.ASCII.GetString(rawBytes, index, 4)
                        };

                        if (ce.Signature != signature) {
                            break;
                        }

                        index += 4;

                        // skip 4 unknown
                        index += 4;

                        var ceDataSize = BitConverter.ToUInt32(rawBytes, index);
                        index += 4;

                        ce.PathSize = BitConverter.ToUInt16(rawBytes, index);
                        index += 2;

                        ce.Path = Encoding.Unicode.GetString(rawBytes, index, ce.PathSize).Replace(@"\??\", "");
                        index += ce.PathSize;

                        var packageLen = BitConverter.ToUInt16(rawBytes, index);
                        index += 2;
                        //skip package data
                        index += packageLen;

                        // skip 4 unknown (insertion flags?)
                        ce.InsertFlags = (AppCompatCacheModule.InsertFlag)BitConverter.ToInt32(rawBytes, index);
                        index += 4;

                        // skip 4 unknown (shim flags?)
                        index += 4;

                        ce.LastModifiedTimeUTC =
                            DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();

                        if (ce.LastModifiedTimeUTC.Value.Year == 1601) {
                            ce.LastModifiedTimeUTC = null;
                        }

                        index += 8;

                        ce.DataSize = BitConverter.ToInt32(rawBytes, index);
                        index += 4;

                        ce.Data = rawBytes.Skip(index).Take(ce.DataSize).ToArray();
                        index += ce.DataSize;

                        if ((ce.InsertFlags & AppCompatCacheModule.InsertFlag.Executed) == AppCompatCacheModule.InsertFlag.Executed) {
                            ce.Executed = AppCompatCacheModule.Execute.Yes;
                        } else {
                            ce.Executed = AppCompatCacheModule.Execute.No;
                        }

                        ce.ControlSet = controlSet;

                        ce.CacheEntryPosition = position;

                        Entries.Add(ce);
                        position += 1;
                    } catch (Exception ex) {

                        //TODO report this
                        //take what we can get
                        break;
                    }
                }
            }

            public List<CacheEntry> Entries { get; }
            public int EntryCount { get; }
            public int ControlSet { get; }
        }
    
        public class Windows10 : IAppCompatCache {
            public int ExpectedEntries { get; }

            public Windows10(byte[] rawBytes, int controlSet) {
                Entries = new List<CacheEntry>();

                ExpectedEntries = 0;

                var offsetToRecords = BitConverter.ToInt32(rawBytes, 0);

                ExpectedEntries = BitConverter.ToInt32(rawBytes, 0x24);

                if (offsetToRecords == 0x34) {
                    ExpectedEntries = BitConverter.ToInt32(rawBytes, 0x28);
                }

                var index = offsetToRecords;
                ControlSet = controlSet;

                EntryCount = -1;

                var position = 0;

                while (index < rawBytes.Length) {
                    try {
                        var ce = new CacheEntry {
                            Signature = Encoding.ASCII.GetString(rawBytes, index, 4)
                        };

                        if (ce.Signature != "10ts") {
                            break;
                        }

                        index += 4;

                        // skip 4 unknown
                        index += 4;

                        var ceDataSize = BitConverter.ToUInt32(rawBytes, index);
                        index += 4;

                        ce.PathSize = BitConverter.ToUInt16(rawBytes, index);
                        index += 2;
                        ce.Path = Encoding.Unicode.GetString(rawBytes, index, ce.PathSize).Replace(@"\??\", "");
                        index += ce.PathSize;

                        ce.LastModifiedTimeUTC =
                            DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();

                        if (ce.LastModifiedTimeUTC.Value.Year == 1601) {
                            ce.LastModifiedTimeUTC = null;
                        }

                        index += 8;

                        ce.DataSize = BitConverter.ToInt32(rawBytes, index);
                        index += 4;

                        ce.Data = rawBytes.Skip(index).Take(ce.DataSize).ToArray();
                        index += ce.DataSize;

                        ce.Executed = Execute.NA;

                        ce.ControlSet = controlSet;
                        ce.CacheEntryPosition = position;

                        Entries.Add(ce);
                        position += 1;
                    } catch (Exception ex) { 
                        //TODO Report this
                        //take what we can get
                        break;
                    }
                }
            }

            public List<CacheEntry> Entries { get; }
            public int EntryCount { get; }
            public int ControlSet { get; }
        }
        private OperatingSystemVersion getWindowsVersion(byte[] rawBytes,bool is32) {
            var sigNum = BitConverter.ToUInt32(rawBytes, 0);
            var OperatingSystem = OperatingSystemVersion.Unknown;
            var signature = Encoding.ASCII.GetString(rawBytes, 128, 4);
            if (sigNum == 0xDEADBEEF) //DEADBEEF, WinXp
            {
                OperatingSystem = OperatingSystemVersion.WindowsXP;

            } else if (sigNum == 0xbadc0ffe) {
                OperatingSystem = OperatingSystemVersion.WindowsVistaWin2k3Win2k8;
            } else if (sigNum == 0xBADC0FEE) { //BADC0FEE, Win7
                if (is32) {
                    OperatingSystem = OperatingSystemVersion.Windows7x86;
                } else {
                    OperatingSystem = OperatingSystemVersion.Windows7x64_Windows2008R2;
                }

            } else if (signature == "00ts") {
                OperatingSystem = OperatingSystemVersion.Windows80_Windows2012;
 
            } else if (signature == "10ts") {
                OperatingSystem = OperatingSystemVersion.Windows81_Windows2012R2;

            } else {
                //is it windows 10?

                var offsetToEntries = BitConverter.ToInt32(rawBytes, 0);

                OperatingSystem = OperatingSystemVersion.Windows10;

                if (offsetToEntries == 0x34) {
                    OperatingSystem = OperatingSystemVersion.Windows10Creators;
                }

                signature = Encoding.ASCII.GetString(rawBytes, offsetToEntries, 4);
                if (signature == "10ts") {
                    OperatingSystem = OperatingSystemVersion.Windows10;
                }
            }

            return OperatingSystem;
        }

        int getControlSet() {
            var keyCurrUser = Microsoft.Win32.Registry.LocalMachine;
            var subKey2 = keyCurrUser.OpenSubKey(@"SYSTEM\Select");
            var ControlSet = (int)subKey2.GetValue("Current");
            return ControlSet;
        }
        public void run() {
            byte[] rawBytes = readBytes();
            bool is32bit = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"));
            var controlSet = getControlSet();
            var operatingSystem = getWindowsVersion(rawBytes,is32bit);
            IAppCompatCache appCache;
            if (operatingSystem == OperatingSystemVersion.Windows10) {
                appCache = new Windows10(rawBytes, controlSet);
            } else if (operatingSystem == OperatingSystemVersion.Windows10Creators) {
                appCache = new Windows10(rawBytes, controlSet);
            } else if (operatingSystem == OperatingSystemVersion.Windows7x86) {
                appCache = new Windows7(rawBytes, is32bit, controlSet);
            } else if (operatingSystem == OperatingSystemVersion.Windows7x64_Windows2008R2) {
                appCache = new Windows7(rawBytes, is32bit, controlSet);
            } else if (operatingSystem == OperatingSystemVersion.Windows80_Windows2012) {
                var os = OperatingSystemVersion.Windows80_Windows2012;
                appCache = new Windows8x(rawBytes, os, controlSet);
            } else if (operatingSystem == OperatingSystemVersion.Windows81_Windows2012R2) {
                var os = OperatingSystemVersion.Windows81_Windows2012R2;
                appCache = new Windows8x(rawBytes, os, controlSet);
            } else if (operatingSystem == OperatingSystemVersion.WindowsVistaWin2k3Win2k8) {
                appCache = new VistaWin2k3Win2k8(rawBytes, is32bit, controlSet);
            } else if (operatingSystem == OperatingSystemVersion.WindowsXP) {
                appCache = new WindowsXP(rawBytes, is32bit, controlSet);
            }
            return;
        }
    }
}
