//
//  SPDX-FileName: NeosAssemblyResolver.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Mono.Cecil;

namespace Remora.Neos.Headless;

/// <summary>
/// Defines functionality to assist with resolution of NeosVR assemblies.
/// </summary>
public class NeosAssemblyResolver : DefaultAssemblyResolver, IDisposable
{
    private readonly IReadOnlyList<string> _additionalSearchPaths;
    private readonly IReadOnlyDictionary<string, string[]> _knownNativeLibraryMappings = new Dictionary<string, string[]>
    {
        { "assimp", new[] { "libassimp.so.5" } },
        { "freeimage", new[] { "libfreeimage.so.3" } },
        { "freetype6", new[] { "libfreetype.so.6" } },
        { "opus", new[] { "libopus.so.0" } },
        { "dl", new[] { "libdl.so.2" } },
        { "libdl.so", new[] { "libdl.so.2" } },
        { "zlib", new[] { "libzlib.so.1" } },
    };

    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeosAssemblyResolver"/> class.
    /// </summary>
    /// <param name="additionalSearchPaths">The additional search paths to look in for native libraries.</param>
    public NeosAssemblyResolver(IReadOnlyList<string> additionalSearchPaths)
    {
        _additionalSearchPaths = additionalSearchPaths;

        foreach (var context in AssemblyLoadContext.All)
        {
            context.ResolvingUnmanagedDll += ResolveNativeAssembly;
        }

        AppDomain.CurrentDomain.AssemblyLoad += AddResolverToAssembly;
        AppDomain.CurrentDomain.AssemblyResolve += ResolveManagedAssembly;

        this.ResolveFailure += ResolveCecilAssembly;
    }

    private AssemblyDefinition? ResolveCecilAssembly(object sender, AssemblyNameReference reference)
    {
        // We only handle non-system assemblies
        return reference.FullName.StartsWith("System")
            ? null
            : SearchDirectory(reference, _additionalSearchPaths, new ReaderParameters());
    }

    private void AddResolverToAssembly(object? sender, AssemblyLoadEventArgs args)
    {
        var context = AssemblyLoadContext.GetLoadContext(args.LoadedAssembly);
        if (context is null || context == AssemblyLoadContext.Default)
        {
            return;
        }

        context.ResolvingUnmanagedDll += ResolveNativeAssembly;
    }

    private IntPtr ResolveNativeAssembly(Assembly sourceAssembly, string assemblyName)
    {
        var filename = assemblyName.Contains(".so") || assemblyName.Contains(".dll")
            ? assemblyName
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? $"lib{assemblyName}.so"
                : RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? $"{assemblyName}.dll"
                    : assemblyName;

        // first, try loading it verbatim from the additional paths
        foreach (var additionalSearchPath in _additionalSearchPaths)
        {
            var libraryPath = Path.Combine(additionalSearchPath, filename);
            if (NativeLibrary.TryLoad(libraryPath, out var additionalPathHandle))
            {
                return additionalPathHandle;
            }
        }

        // then check through the normal system methods
        if (NativeLibrary.TryLoad(assemblyName, out var handle))
        {
            return handle;
        }

        var asLower = assemblyName.ToLowerInvariant();
        if (NativeLibrary.TryLoad(asLower, out handle))
        {
            return handle;
        }

        if (!_knownNativeLibraryMappings.TryGetValue(asLower, out var candidates))
        {
            return IntPtr.Zero;
        }

        foreach (var candidate in candidates)
        {
            if (NativeLibrary.TryLoad(candidate, out handle))
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    private Assembly? ResolveManagedAssembly(object? sender, ResolveEventArgs args)
    {
        // check if it's already loaded
        var existing = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
        if (existing is not null)
        {
            return existing;
        }

        var filename = args.Name.Split(',')[0] + ".dll".ToLower();

        // then, try loading it verbatim from the additional paths
        foreach (var additionalSearchPath in _additionalSearchPaths)
        {
            var libraryPath = Path.Combine(additionalSearchPath, filename);
            if (File.Exists(libraryPath))
            {
                return Assembly.LoadFile(libraryPath);
            }
        }

        return null;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_isDisposed)
        {
            return;
        }

        GC.SuppressFinalize(this);

        foreach (var context in AssemblyLoadContext.All)
        {
            context.ResolvingUnmanagedDll -= ResolveNativeAssembly;
        }

        AppDomain.CurrentDomain.AssemblyLoad -= AddResolverToAssembly;
        AppDomain.CurrentDomain.AssemblyResolve -= ResolveManagedAssembly;
        _isDisposed = true;
    }
}
