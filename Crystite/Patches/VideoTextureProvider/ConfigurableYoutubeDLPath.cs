//
//  SPDX-FileName: ConfigurableYoutubeDLPath.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;

namespace Crystite.Patches.VideoTextureProvider;

/// <summary>
/// Replaces hardcoded paths to youtube-dl with a configurable option.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.VideoTextureProvider))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class ConfigurableYoutubeDLPath
{
    /// <summary>
    /// Gets or sets a value indicating whether Youtube-DL integration should be enabled.
    /// </summary>
    public static bool EnableYoutubeDL { get; set; } = true;

    /// <summary>
    /// Gets or sets the path to the Youtube-DL executable.
    /// </summary>
    public static string YoutubeDLPath { get; set; } = null!;

    /// <summary>
    /// Gets the target method, unwrapping the async wrapper if required.
    /// </summary>
    /// <returns>The target method.</returns>
    [HarmonyTargetMethod]
    public static MethodInfo GetTargetMethod()
    {
        var method = AccessTools.Method(typeof(FrooxEngine.VideoTextureProvider), "LoadFromVideoServiceIntern");
        if (method is null)
        {
            throw new InvalidOperationException();
        }

        var asyncAttribute = method.GetCustomAttribute<AsyncStateMachineAttribute>();
        if (asyncAttribute is null)
        {
            return method;
        }

        var asyncStateMachineType = asyncAttribute.StateMachineType;
        var asyncMethodBody = AccessTools.DeclaredMethod(asyncStateMachineType, nameof(IAsyncStateMachine.MoveNext))
                              ?? throw new InvalidOperationException();

        return asyncMethodBody;
    }

    /// <summary>
    /// Replaces hardcoded paths to youtube-dl with a configurable option.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <param name="generator">The IL generator.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler
    (
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        var enumeratedInstructions = instructions.ToArray();

        var pathGetter = AccessTools.PropertyGetter(typeof(ConfigurableYoutubeDLPath), nameof(YoutubeDLPath))
                         ?? throw new InvalidOperationException();

        var youtubeDLConstructor = AccessTools.Constructor
        (
            Type.GetType("NYoutubeDL.YoutubeDL, NYoutubeDL"),
            new[] { typeof(string) }
        ) ?? throw new InvalidOperationException();

        var fileExists = AccessTools.Method(typeof(File), nameof(File.Exists))
                         ?? throw new InvalidOperationException();

        var youtubeDLField = AccessTools.Field(typeof(FrooxEngine.VideoTextureProvider), "youtubeDL");

        var leaveInstruction = enumeratedInstructions
            .First(i => i.opcode == OpCodes.Leave || i.opcode == OpCodes.Leave_S);

        var hasPatchedInitialCheck = false;
        for (var index = 0; index < enumeratedInstructions.Length; index++)
        {
            var instruction = enumeratedInstructions[index];

            if (instruction.LoadsField(youtubeDLField))
            {
                if (hasPatchedInitialCheck)
                {
                    // already done; must be something else
                    yield return instruction;
                    continue;
                }

                var nextInstruction = index + 1 < enumeratedInstructions.Length
                        ? enumeratedInstructions[index + 1]
                        : null;

                if (nextInstruction is null)
                {
                    // last instruction
                    yield return instruction;
                    continue;
                }

                if (nextInstruction.opcode != OpCodes.Brtrue_S && nextInstruction.opcode != OpCodes.Brtrue)
                {
                    // not a null check
                    yield return instruction;
                    continue;
                }

                if (EnableYoutubeDL)
                {
                    // first null check; means we're creating the object
                    yield return instruction;

                    var objectCreationLabel = generator.DefineLabel();

                    // if (!File.Exists(ConfigurableYoutubeDLPath.YoutubeDLPath)
                    yield return new CodeInstruction(OpCodes.Call, pathGetter);
                    yield return new CodeInstruction(OpCodes.Call, fileExists);

                    yield return new CodeInstruction(OpCodes.Brtrue, objectCreationLabel);

                    // return;
                    yield return new CodeInstruction(OpCodes.Leave, leaveInstruction.operand);

                    // youtubeDL = new YoutubeDL(ConfigurableYoutubeDLPath.YoutubeDLPath);
                    yield return new CodeInstruction(OpCodes.Call, pathGetter)
                        .WithLabels(objectCreationLabel);
                    yield return new CodeInstruction(OpCodes.Newobj, youtubeDLConstructor);
                    yield return new CodeInstruction(OpCodes.Stfld, youtubeDLField);
                }
                else
                {
                    // return;
                    yield return new CodeInstruction(OpCodes.Leave, leaveInstruction.operand);
                }

                var jumpLabel = (Label)nextInstruction.operand;
                var jumpTarget = enumeratedInstructions.First(i => i.labels.Contains(jumpLabel));
                var jumpIndex = Array.IndexOf(enumeratedInstructions, jumpTarget);

                // skip the original code
                index = jumpIndex - 1;
                hasPatchedInitialCheck = true;
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
