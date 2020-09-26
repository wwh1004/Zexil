using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Zexil.DotNet.Emulation.Internal {
	/// <summary>
	/// Platform Adaptation Layer
	/// </summary>
	internal static class Pal {
		private enum Platform : byte {
			Windows,
			Unix
		}

		private const Platform _platform = Platform.Windows;

		public static nint MapFile(string filePath, bool mapAsImage) {
#pragma warning disable IDE0066 // Convert switch statement to expression
			switch (_platform) {
#pragma warning restore IDE0066 // Convert switch statement to expression
			case Platform.Windows: return Windows.MapFile(filePath, mapAsImage);
			default: throw new PlatformNotSupportedException();
			}
		}

		public static void UnmapFile(nint address) {
			switch (_platform) {
			case Platform.Windows: Windows.UnmapFile(address); break;
			default: throw new PlatformNotSupportedException();
			}
		}

		public static nint AllocMemory(uint size, bool executable) {
#pragma warning disable IDE0066 // Convert switch statement to expression
			switch (_platform) {
#pragma warning restore IDE0066 // Convert switch statement to expression
			case Platform.Windows: return Windows.Alloc(size, executable);
			default: throw new PlatformNotSupportedException();
			}
		}

		public static void FreeMemory(nint address) {
			switch (_platform) {
			case Platform.Windows: Windows.Free(address); break;
			default: throw new PlatformNotSupportedException();
			}
		}

		private static unsafe class Windows {
			private const uint GENERIC_READ = 0x80000000;
			private const uint FILE_SHARE_READ = 0x00000001;
			private const uint OPEN_EXISTING = 3;
			private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
			private const uint PAGE_READONLY = 0x02;
			private const uint SEC_IMAGE = 0x1000000;
			private const uint SECTION_MAP_READ = 0x0004;
			private const uint FILE_MAP_READ = SECTION_MAP_READ;

			[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
			private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, nint lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, nint hTemplateFile);

			[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
			private static extern SafeFileHandle CreateFileMapping(SafeFileHandle hFile, nint lpAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

			[DllImport("kernel32.dll", SetLastError = true)]
			private static extern nint MapViewOfFile(SafeFileHandle hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, nint dwNumberOfBytesToMap);

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool UnmapViewOfFile(nint lpBaseAddress);

			// from dnlib.IO.MemoryMappedDataReaderFactory.Windows
			public static nint MapFile(string filePath, bool mapAsImage) {
				using var fileHandle = CreateFile(filePath, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
				if (fileHandle.IsInvalid)
					throw new Win32Exception();
				using var fileMapping = CreateFileMapping(fileHandle, 0, PAGE_READONLY | (mapAsImage ? SEC_IMAGE : 0), 0, 0, null);
				if (fileMapping.IsInvalid)
					throw new Win32Exception();
				nint data = MapViewOfFile(fileMapping, FILE_MAP_READ, 0, 0, 0);
				if (data == 0)
					throw new Win32Exception();
				return data;
			}

			// from dnlib.IO.MemoryMappedDataReaderFactory.Windows
			public static void UnmapFile(nint address) {
				if (!UnmapViewOfFile(address))
					throw new Win32Exception();
			}

			private const uint PAGE_EXECUTE_READWRITE = 0x40;

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool VirtualProtect(nint lpAddress, uint dwSize, uint flNewProtect, uint* lpflOldProtect);

			public static nint Alloc(uint size, bool executable) {
				nint address = Marshal.AllocHGlobal((int)size);
				if (address == 0)
					throw new Win32Exception();
				uint oldProtect;
				if (executable && !VirtualProtect(address, size, PAGE_EXECUTE_READWRITE, &oldProtect))
					throw new Win32Exception();
				return address;
			}

			public static void Free(nint address) {
				Marshal.FreeHGlobal(address);
			}
		}
	}
}
