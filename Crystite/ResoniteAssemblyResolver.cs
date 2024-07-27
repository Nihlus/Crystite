//
//  SPDX-FileName: ResoniteAssemblyResolver.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Mono.Cecil;

namespace Crystite;

/// <summary>
/// Defines functionality to assist with resolution of Resonite assemblies.
/// </summary>
public class ResoniteAssemblyResolver : DefaultAssemblyResolver
{
    private readonly IReadOnlyList<string> _additionalSearchPaths;
    private readonly IReadOnlyDictionary<string, string[]> _knownNativeLibraryMappings = new Dictionary<string, string[]>
    {
        { "assimp", new[] { "libassimp.so.5", "Assimp64.so" } },
        { "freeimage", new[] { "libfreeimage.so.3", "libFreeImage.so" } },
        { "freetype6", new[] { "libfreetype.so.6", "libfreetype6.so" } },
        { "opus", new[] { "libopus.so.0", "libopus.so" } },
        { "dl", new[] { "libdl.so.2" } },
        { "libdl.so", new[] { "libdl.so.2" } },
        { "zlib", new[] { "libz.so.1", "libzlib.so.1", "libzlib.so" } },
    };

    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResoniteAssemblyResolver"/> class.
    /// </summary>
    /// <param name="additionalSearchPaths">The additional search paths to look in for native libraries.</param>
    public ResoniteAssemblyResolver(IReadOnlyList<string> additionalSearchPaths)
    {
        var paths = new List<string>(additionalSearchPaths);

        // Add some unity-specific search paths for desktop client compatibility
        paths.InsertRange(0, additionalSearchPaths.Select(p => Path.Combine(p, "Resonite_Data", "Managed")));
        paths.InsertRange(0, additionalSearchPaths.Select(p => Path.Combine(p, "Resonite_Data", "Plugins", "x86_64")));
        paths.InsertRange(0, additionalSearchPaths.Select(p => Path.Combine(p, "Resonite_Data", "Plugins")));

        var ourLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (ourLocation is not null)
        {
            // our files always take priority
            paths.Insert(0, ourLocation);
        }

        _additionalSearchPaths = paths;

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
        if (assemblyName is "__Internal")
        {
            return NativeLibrary.GetMainProgramHandle();
        }

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

        return candidates.Any(candidate => NativeLibrary.TryLoad(candidate, out handle))
            ? handle
            : IntPtr.Zero;
    }

    private Assembly? ResolveManagedAssembly(object? sender, ResolveEventArgs args)
    {
        // check if it's already loaded
        var existing = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.FullName is not null && a.FullName.Contains(args.Name));

        if (existing is not null)
        {
            return existing;
        }

        var filename = args.Name.Split(',')[0] + ".dll".ToLower();

        // then, try loading it verbatim from the additional paths
        Assembly? potentialAssembly = null;
        foreach (var additionalSearchPath in _additionalSearchPaths)
        {
            var libraryPath = Path.Combine(additionalSearchPath, filename);
            if (!File.Exists(libraryPath))
            {
                continue;
            }

            var assembly = Assembly.LoadFrom(libraryPath);
            if (assembly.FullName == args.Name)
            {
                // exact match, prefer this to keep things like strong-naming consistent
                return assembly;
            }

            potentialAssembly = assembly;
        }

        return potentialAssembly;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_isDisposed)
        {
            return;
        }

        foreach (var context in AssemblyLoadContext.All)
        {
            context.ResolvingUnmanagedDll -= ResolveNativeAssembly;
        }

        AppDomain.CurrentDomain.AssemblyLoad -= AddResolverToAssembly;
        AppDomain.CurrentDomain.AssemblyResolve -= ResolveManagedAssembly;

        this.ResolveFailure -= ResolveCecilAssembly;
        _isDisposed = true;
    }
}
