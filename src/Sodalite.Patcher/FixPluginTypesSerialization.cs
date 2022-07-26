/*
 * Modified version of: https://github.com/xiaoxiao921/FixPluginTypesSerialization
 * Licensed under LGPL-3
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Sodalite.Patcher;

public static class FixPluginTypesSerialization
{
	// Delegate types for the native functions we're hooking
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void AwakeFromLoadDelegate(IntPtr monoManager, int awakeMode);
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate bool ReadStringFromFileDelegate(IntPtr outData, IntPtr assemblyStringPathName);
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate bool IsFileCreatedDelegate(IntPtr path);
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate IntPtr StringAssignType(IntPtr ptr, string str, ulong len);

	// Generated trampolines to call the original function from our hooks
	private static AwakeFromLoadDelegate _origAwakeFromLoad = null!;
	private static ReadStringFromFileDelegate _origReadStringFromFile = null!;
	private static IsFileCreatedDelegate _origIsFileCreated = null!;
	private static StringAssignType _assignNativeString = null!;

	// Created detours so we can dispose of them when we're done
	private static NativeDetour? _detourReadStringFromFile;
	private static NativeDetour? _detourIsFileCreated;

	// List of plugins installed
	private static readonly List<string> PluginPaths = new();

	public static void ApplyHooks()
	{
		// Get a list of all the BepInEx plugin assemblies, which we will need later
		foreach (var file in Directory.GetFiles(BepInEx.Paths.PluginPath, "*.dll", SearchOption.AllDirectories))
		{
			// Check if it's actually a valid assembly
			try
			{
				AssemblyName.GetAssemblyName(file);
				PluginPaths.Add(file);
			}
			catch (BadImageFormatException)
			{
				// Ignored.
			}
		}

		// Get the base address of the running process
		IntPtr gameBase = DynDll.OpenLibrary("h3vr.exe");

		// Create a delegate for core::StringStorageDefault<char>::assign so we can use it in a hook later
		// This function assigns a string value to Unity's own string struct
		_assignNativeString = (StringAssignType) Marshal.GetDelegateForFunctionPointer((IntPtr) (gameBase.ToInt64() + 0x471A0), typeof(StringAssignType));

		// Hook MonoManager::AwakeFromLoad
		IntPtr awakeFromLoad = (IntPtr) (gameBase.ToInt64() + 0x83D640);
		var onAwakeFromLoad = Marshal.GetFunctionPointerForDelegate(new AwakeFromLoadDelegate(OnAwakeFromLoad));
		var detourAwakeFromLoad = new NativeDetour(awakeFromLoad, onAwakeFromLoad, new NativeDetourConfig {ManualApply = true});
		_origAwakeFromLoad = detourAwakeFromLoad.GenerateTrampoline<AwakeFromLoadDelegate>();
		detourAwakeFromLoad.Apply();

		// Hook ReadStringFromFile
		IntPtr readStringFromFile = (IntPtr) (gameBase.ToInt64() + 0x711650);
		var onReadStringFromFile = Marshal.GetFunctionPointerForDelegate(new ReadStringFromFileDelegate(OnReadStringFromFile));
		_detourReadStringFromFile = new NativeDetour(readStringFromFile, onReadStringFromFile, new NativeDetourConfig {ManualApply = true});
		_origReadStringFromFile = _detourReadStringFromFile.GenerateTrampoline<ReadStringFromFileDelegate>();
		_detourReadStringFromFile.Apply();

		// Hook IsFileCreated
		IntPtr isFileCreated = (IntPtr) (gameBase.ToInt64() + 0x712A50);
		var onIsFileCreated = Marshal.GetFunctionPointerForDelegate(new IsFileCreatedDelegate(OnIsFileCreated));
		_detourIsFileCreated = new NativeDetour(isFileCreated, onIsFileCreated, new NativeDetourConfig {ManualApply = true});
		_origIsFileCreated = _detourIsFileCreated.GenerateTrampoline<IsFileCreatedDelegate>();
		_detourIsFileCreated.Apply();
	}

	/// <summary>
	/// The MonoManager is the global game manager responsible for managing the assemblies.
	/// What we want to do here is intercept it's AwakeFromLoad function and inject all of the BepInEx plugin assemblies
	/// into it's internal assembly names list.
	/// </summary>
	private static void OnAwakeFromLoad(IntPtr monoManagerPtr, int awakeMode)
	{
		// Use the helper class to do the three important things:
		// 1. Copy the existing assembly list into our domain
		// 2. Append the BepInEx plugin assembly names to this copied list
		// 3. Write the copied and appended list back into the native memory
		// * We can't append to the list directly because this list needs to be in one single block of
		//   memory, thus we need to reallocate the entire thing.
		var monoManager = new MonoManager(monoManagerPtr);
		monoManager.CopyNativeAssemblyListToManaged();
		monoManager.AddAssembliesToManagedList(PluginPaths);
		monoManager.AllocNativeAssemblyListFromManaged();

		// Let the original function run
		_origAwakeFromLoad(monoManagerPtr, awakeMode);

		// Dispose of the two other detours as they are no longer needed
		_detourReadStringFromFile?.Dispose();
		_detourIsFileCreated?.Dispose();
	}

	/// <summary>
	/// Honestly I'm not fully sure where this part is used, but here we fix the path to the dll files.
	/// Since Unity expects them all in Managed/, and we told it that they _were_ in Managed/ we need to
	/// intercept it loading the file and redirect it to the actual path of the DLL.
	/// </summary>
	private static bool OnReadStringFromFile(IntPtr outData, IntPtr assemblyStringPathName)
	{
		// Get where Unity thinks the path is
		string assemblyPath = ReadNativeString(assemblyStringPathName);
		if (!string.IsNullOrEmpty(assemblyPath))
		{
			// Check if it's one of ours
			string fileName = Path.GetFileName(assemblyPath);
			string? newPath = PluginPaths.FirstOrDefault(p => Path.GetFileName(p) == fileName);

			// If it is, change the path to be where it actually is rather than where Unity thinks it is.
			if (!string.IsNullOrEmpty(newPath))
			{
				_assignNativeString(assemblyStringPathName, newPath, (ulong) newPath.Length);
			}
		}

		// Run the original function with the (possibly) modified assembly path
		return _origReadStringFromFile(outData, assemblyStringPathName);
	}

	/// <summary>
	/// This is the function Unity uses to check if a file exists. It's used for more than just DLL files, but that's all we care about from it.
	/// Unity expects all DLL files to be in the Managed/ folder of the game directory, but with BepInEx plugins that is not the case.
	/// So here, all we want to do is trick Unity into thinking that they do exist in the Managed/ folder.
	/// </summary>
	private static bool OnIsFileCreated(IntPtr path)
	{
		// If Unity wants to know if one of the plugin assemblies exist in the managed folder, pretend it does.
		string fileName = Path.GetFileName(ReadNativeString(path));
		if (PluginPaths.Any(p => Path.GetFileName(p) == fileName)) return true;

		// Otherwise let it do it's thing
		return _origIsFileCreated(path);
	}

	/// <summary>
	/// Reads the value from a native string struct from unmanaged memory
	/// </summary>
	/// <param name="ptr">The pointer to the structure</param>
	/// <returns>The string value of the struct</returns>
	private static string ReadNativeString(IntPtr ptr)
	{
		// Get the pointer to the string in memory
		var stringPointer = Marshal.ReadIntPtr(ptr);
		if (stringPointer == IntPtr.Zero)
		{
			// If the pointer is null, that means it's stored in the struct directly.
			// In this format, the string is 16 chars or less.
			var length = Marshal.ReadInt64(ptr, 24);
			return Marshal.PtrToStringAnsi((IntPtr) (ptr.ToInt64() + 8), (int) length);
		}
		else
		{
			// If it isn't null, we can just go out into that memory location and read it.
			var length = Marshal.ReadInt64(ptr, 8);
			return Marshal.PtrToStringAnsi(stringPointer, (int) length);
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	private struct AssemblyStringStruct
	{
		public const int ValidStringLabel = 0x42;

		public nint data;
		public ulong capacity;
		public ulong extra;
		public ulong size;
		public int label;
	}

	[StructLayout(LayoutKind.Sequential)]
	private unsafe struct AssemblyList
	{
		public AssemblyStringStruct* first;
		public AssemblyStringStruct* last;
		public AssemblyStringStruct* end;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct MonoManagerStruct
	{
		[FieldOffset(0x1E8)] public AssemblyList m_AssemblyNames;
	}

	private unsafe class MonoManager
	{
		public MonoManager(IntPtr pointer)
		{
			_this = (MonoManagerStruct*) pointer;
		}

		private readonly MonoManagerStruct* _this;

		private readonly List<AssemblyStringStruct> _managedAssemblyList = new();

		public void CopyNativeAssemblyListToManaged()
		{
			_managedAssemblyList.Clear();

			for (AssemblyStringStruct* s = _this->m_AssemblyNames.first; s != _this->m_AssemblyNames.last; s++)
			{
				var newAssemblyString = new AssemblyStringStruct
				{
					capacity = s->capacity,
					extra = s->extra,
					label = s->label,
					size = s->size,
					data = s->data
				};

				_managedAssemblyList.Add(newAssemblyString);
			}
		}

		public void AddAssembliesToManagedList(List<string> pluginAssemblyPaths)
		{
			foreach (var pluginAssemblyPath in pluginAssemblyPaths)
			{
				var pluginAssemblyName = Path.GetFileName(pluginAssemblyPath);
				var length = (ulong) pluginAssemblyName.Length;

				var assemblyString = new AssemblyStringStruct
				{
					label = AssemblyStringStruct.ValidStringLabel,
					data = Marshal.StringToHGlobalAnsi(pluginAssemblyName),
					capacity = length,
					size = length
				};

				_managedAssemblyList.Add(assemblyString);
			}
		}

		public void AllocNativeAssemblyListFromManaged()
		{
			var nativeArray = (AssemblyStringStruct*) Marshal.AllocHGlobal(Marshal.SizeOf(typeof(AssemblyStringStruct)) * _managedAssemblyList.Count);

			var i = 0;
			for (AssemblyStringStruct* s = nativeArray; i < _managedAssemblyList.Count; s++, i++)
			{
				s->label = _managedAssemblyList[i].label;
				s->size = _managedAssemblyList[i].size;
				s->capacity = _managedAssemblyList[i].capacity;
				s->extra = _managedAssemblyList[i].extra;
				s->data = _managedAssemblyList[i].data;
			}

			_this->m_AssemblyNames.first = nativeArray;
			_this->m_AssemblyNames.last = nativeArray + _managedAssemblyList.Count;
			_this->m_AssemblyNames.end = _this->m_AssemblyNames.last;
		}
	}
}
