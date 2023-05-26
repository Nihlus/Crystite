//
//  SPDX-FileName: RedirectCommandLineParsing.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Remora.Neos.Headless.Configuration;

namespace Remora.Neos.Headless.Patches.Engine;

/// <summary>
/// Patches the <see cref="Engine"/> class.
/// </summary>
[HarmonyPatch("FrooxEngine.Engine+<Initialize>d__298", "MoveNext")]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class RedirectCommandLineParsing
{
    private static readonly FieldInfo _appPathField = AccessTools.Field
    (
        "FrooxEngine.Engine+<Initialize>d__298:appPath"
    );

    /// <summary>
    /// Gets the command line arguments visible to the <see cref="Engine"/> instance.
    /// </summary>
    public static string[] RedirectedCommandLineArgs { get; internal set; } = Array.Empty<string>();

    /// <summary>
    /// Replaces a hard-coded path with a reference to an input argument.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getArgsMethod = typeof(Environment).GetMethod(nameof(Environment.GetCommandLineArgs));
        var getRedirectedArgsMethod = typeof(RedirectCommandLineParsing)
            .GetProperty(nameof(RedirectedCommandLineArgs))?
            .GetGetMethod();

        foreach (var instruction in instructions)
        {
            if (instruction.Calls(getArgsMethod))
            {
                yield return instruction.Clone(getRedirectedArgsMethod);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    /// <summary>
    /// Sets the redirected command line arguments based on the provided config.
    /// </summary>
    /// <param name="config">The config to read from.</param>
    public static void SetRedirectedCommandLine(NeosHeadlessConfig config)
    {
        var args = new List<string>();

        if (config.PluginAssemblies is not null)
        {
            foreach (var pluginAssembly in config.PluginAssemblies)
            {
                args.Add("LoadAssembly");
                args.Add(pluginAssembly);
            }
        }

        if (config.GeneratePreCache is true)
        {
            args.Add("GeneratePreCache");
        }

        if (config.BackgroundWorkers is { } backgroundWorkers)
        {
            args.Add("backgroundworkers");
            args.Add(backgroundWorkers.ToString());
        }

        if (config.PriorityWorkers is { } priorityWorkers)
        {
            args.Add("priorityworkers");
            args.Add(priorityWorkers.ToString());
        }

        RedirectedCommandLineArgs = args.ToArray();
    }
}
