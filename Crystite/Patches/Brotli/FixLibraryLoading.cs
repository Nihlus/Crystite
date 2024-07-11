//
//  SPDX-FileName: FixLibraryLoading.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace Crystite.Patches.Brotli;

/// <summary>
/// Fixes library loading for brotli, correctly splitting the bindings into the two actual library files on disk.
/// </summary>
[HarmonyPatch]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class FixLibraryLoading
{
    /// <summary>
    /// Gets a value indicating whether the patch applied cleanly.
    /// </summary>
    public static bool Applied { get; private set; }

    private static readonly Assembly _brotliCore = AppDomain.CurrentDomain.Load("Brotli.Core");
    private static MethodInfo _fillDelegate = null!;
    private static ConstructorInfo _nativeLoaderConstructor = null!;

    /// <summary>
    /// Prepares various auxiliary data used by the patch.
    /// </summary>
    [HarmonyPrepare]
    private static void Prepare()
    {
        var nativeLoader = _brotliCore.GetType("Brotli.NativeLibraryLoader")
                           ?? throw new InvalidOperationException();

        _fillDelegate = nativeLoader.GetMethods().Single(m => m.Name is "FillDelegate")
               ?? throw new InvalidOperationException();

        _nativeLoaderConstructor = nativeLoader.GetConstructor
        (
            BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(string) }
        ) ?? throw new InvalidOperationException();
    }

    /// <summary>
    /// Gets the target methods to patch.
    /// </summary>
    /// <returns>The methods.</returns>
    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> GetTargetMethods()
    {
        var brolib32 = _brotliCore.GetType("Brotli.Brolib32")
                       ?? throw new InvalidOperationException();

        yield return brolib32.GetConstructor(BindingFlags.NonPublic | BindingFlags.Static, Array.Empty<Type>())
                     ?? throw new InvalidOperationException();

        var brolib64 = _brotliCore.GetType("Brotli.Brolib64")
                       ?? throw new InvalidOperationException();

        yield return brolib64.GetConstructor(BindingFlags.NonPublic | BindingFlags.Static, Array.Empty<Type>())
                     ?? throw new InvalidOperationException();
    }

    /// <summary>
    /// Transpiles the target method.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var enumeratedInstructions = instructions.ToImmutableArray();

        var loadCalls = enumeratedInstructions.Where(i => i.opcode == OpCodes.Ldsflda);
        var fillDelegateCalls = enumeratedInstructions.Where
        (
            i => i.operand is MethodInfo { IsGenericMethod: true } method &&
                 method.GetGenericMethodDefinition() == _fillDelegate
        );

        var callPairs = loadCalls.Zip(fillDelegateCalls).ToImmutableArray();
        var encoderCalls = callPairs.Where(p =>
        {
            var fieldName = ((FieldInfo)p.First.operand).Name;
            return fieldName.Contains("BrotliEncoder");
        }).ToImmutableArray();

        var decoderCalls = callPairs.Where(p =>
        {
            var fieldName = ((FieldInfo)p.First.operand).Name;
            return fieldName.Contains("BrotliDecoder");
        }).ToImmutableArray();

        if (encoderCalls.Length <= 0 && decoderCalls.Length <= 0)
        {
            foreach (var instruction in enumeratedInstructions)
            {
                yield return instruction;
            }

            yield break;
        }

        // push the encoder calls
        yield return new CodeInstruction(OpCodes.Ldstr, "libbrotlienc.so.1");
        yield return new CodeInstruction(OpCodes.Newobj, _nativeLoaderConstructor);

        yield return new CodeInstruction(OpCodes.Dup);

        foreach (var encoderCall in encoderCalls)
        {
            yield return encoderCall.First; // ldsflda
            yield return encoderCall.Second; // callvirt

            yield return new CodeInstruction(OpCodes.Dup);
        }

        yield return new CodeInstruction(OpCodes.Pop); // pop the duplicate
        yield return new CodeInstruction(OpCodes.Pop); // pop the original

        // push the decoder calls
        yield return new CodeInstruction(OpCodes.Ldstr, "libbrotlidec.so.1");
        yield return new CodeInstruction(OpCodes.Newobj, _nativeLoaderConstructor);

        yield return new CodeInstruction(OpCodes.Dup);

        foreach (var decoderCall in decoderCalls)
        {
            yield return decoderCall.First; // ldsflda
            yield return decoderCall.Second; // callvirt

            yield return new CodeInstruction(OpCodes.Dup);
        }

        yield return new CodeInstruction(OpCodes.Pop); // pop the duplicate
        yield return new CodeInstruction(OpCodes.Pop); // pop the original

        yield return new CodeInstruction(OpCodes.Ret);

        Applied = true;
    }
}
