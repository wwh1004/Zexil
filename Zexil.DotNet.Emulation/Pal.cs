using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Platform Adaptation Layer
	/// </summary>
	internal static unsafe class Pal {
		private enum Platform : byte {
			Windows,
			Unix
		}

		private const Platform _platform = Platform.Windows;

		public static void* MapFile(string filePath, bool mapAsImage) {
			switch (_platform) {
			case Platform.Windows: return Windows.MapFile(filePath, mapAsImage);
			default: throw new PlatformNotSupportedException();
			}
		}

		public static void UnmapFile(void* address) {
			switch (_platform) {
			case Platform.Windows: Windows.UnmapFile(address); break;
			default: throw new PlatformNotSupportedException();
			}
		}

		public static void* AllocMemory(uint size, bool executable) {
			switch (_platform) {
			case Platform.Windows: return Windows.Alloc(size, executable);
			default: throw new PlatformNotSupportedException();
			}
		}

		public static void FreeMemory(void* address) {
			switch (_platform) {
			case Platform.Windows: Windows.Free(address); break;
			default: throw new PlatformNotSupportedException();
			}
		}

		private static class Windows {
			private const uint GENERIC_READ = 0x80000000;
			private const uint FILE_SHARE_READ = 0x00000001;
			private const uint OPEN_EXISTING = 3;
			private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
			private const uint PAGE_READONLY = 0x02;
			private const uint SEC_IMAGE = 0x1000000;
			private const uint SECTION_MAP_READ = 0x0004;
			private const uint FILE_MAP_READ = SECTION_MAP_READ;

			[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
			private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, void* lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, void* hTemplateFile);

			[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
			private static extern SafeFileHandle CreateFileMapping(SafeFileHandle hFile, void* lpAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

			[DllImport("kernel32.dll", SetLastError = true)]
			private static extern void* MapViewOfFile(SafeFileHandle hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, void* dwNumberOfBytesToMap);

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool UnmapViewOfFile(void* lpBaseAddress);

			// from dnlib.IO.MemoryMappedDataReaderFactory.Windows
			public static void* MapFile(string filePath, bool mapAsImage) {
				using (var fileHandle = CreateFile(filePath, GENERIC_READ, FILE_SHARE_READ, null, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, null)) {
					if (fileHandle.IsInvalid)
						throw new Win32Exception();
					using (var fileMapping = CreateFileMapping(fileHandle, null, PAGE_READONLY | (mapAsImage ? SEC_IMAGE : 0), 0, 0, null)) {
						if (fileMapping.IsInvalid)
							throw new Win32Exception();
						void* data = MapViewOfFile(fileMapping, FILE_MAP_READ, 0, 0, null);
						if (data == null)
							throw new Win32Exception();
						return data;
					}
				}
			}

			// from dnlib.IO.MemoryMappedDataReaderFactory.Windows
			public static void UnmapFile(void* address) {
				if (!UnmapViewOfFile(address))
					throw new Win32Exception();
			}

			private const uint PAGE_EXECUTE_READWRITE = 0x40;

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool VirtualProtect(void* lpAddress, uint dwSize, uint flNewProtect, uint* lpflOldProtect);

			public static void* Alloc(uint size, bool executable) {
				void* address = (void*)Marshal.AllocHGlobal((int)size);
				if (address == null)
					throw new Win32Exception();
				uint oldProtect;
				if (executable && !VirtualProtect(address, size, PAGE_EXECUTE_READWRITE, &oldProtect))
					throw new Win32Exception();
				return address;
			}

			public static void Free(void* address) {
				Marshal.FreeHGlobal((IntPtr)address);
			}
		}
	}
}
