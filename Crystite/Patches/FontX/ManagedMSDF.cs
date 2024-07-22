//
//  SPDX-FileName: ManagedMSDF.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Elements.Assets;
using Elements.Core;
using HarmonyLib;
using JetBrains.Annotations;
using Remora.MSDFGen;
using Remora.MSDFGen.Graphics;

namespace Crystite.Patches.FontX;

/// <summary>
/// Replaces the native MSDF glyph rendering with a managed implementation.
/// </summary>
[HarmonyPatch(typeof(Elements.Assets.FontX), nameof(Elements.Assets.FontX.RenderGlyphMSDF))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class ManagedMSDF
{
    private class SignedDistanceComparer : IComparer<SignedDistance>
    {
        public int Compare(SignedDistance x, SignedDistance y)
        {
            if (x < y)
            {
                return -1;
            }

            if (x > y)
            {
                return 1;
            }

            return 0;
        }
    }

    private static SignedDistanceComparer _distanceComparer = new();

    /// <summary>
    /// Renders a glyph using a managed MSDF implementation.
    /// </summary>
    /// <param name="__instance">The instance.</param>
    /// <param name="___glyphMetrics">The glyph metrics field.</param>
    /// <param name="glyphId">The ID of the glyph.</param>
    /// <param name="bitmap">The output bitmap.</param>
    /// <param name="region">The region to render.</param>
    /// <param name="pixelRange">The range of pixels to render.</param>
    /// <param name="rotated">Whether to render the glyph on its side.</param>
    /// <param name="__result">A value indicating whether the rendering was successful.</param>
    /// <returns>Always false, preventing other prefixes from running.</returns>
    [HarmonyPrefix]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Required")]
    public static bool RenderGlyphMSDF
    (
        Elements.Assets.FontX __instance,
        ConcurrentDictionary<uint, GlyphMetrics> ___glyphMetrics,
        uint glyphId,
        Bitmap2D bitmap,
        Rect region,
        int pixelRange,
        bool rotated,
        out bool __result
    )
    {
        var outline = __instance.GetOutline(glyphId);
        if (outline is null || outline.ContourCount is 0)
        {
            __result = false;
            return false;
        }

        var glyphMetrics = __instance.GetGlyphMetrics(glyphId);
        var glyphSize = new float2(glyphMetrics.width, glyphMetrics.height);

        var val1 = region.size - pixelRange;
        if (rotated)
        {
            val1 = val1.yx;
        }

        var num1 = MathX.MinComponent(val1 / glyphSize);
        var shape = new Shape();
        foreach (var glyphContour in outline)
        {
            var contour = new Contour();
            shape.Contours.Add(contour);

            for (var index = 0; index < glyphContour.SegmentCount; ++index)
            {
                var segment = glyphContour.segments[index];
                var originPoint = glyphContour.GetOriginPoint(index);

                switch (segment.type)
                {
                    case GlyphSegmentType.Line:
                    {
                        contour.Edges.Add(new LinearSegment(originPoint * num1, segment.point * num1, EdgeColor.Black));
                        break;
                    }
                    case GlyphSegmentType.Conic:
                    {
                        contour.Edges.Add
                        (
                            new QuadraticSegment
                            (
                                originPoint * num1,
                                segment.controlPoint0 * num1,
                                segment.point * num1,
                                EdgeColor.Black
                            )
                        );
                        break;
                    }
                    case GlyphSegmentType.Cubic:
                    {
                        contour.Edges.Add
                        (
                            new CubicSegment
                            (
                                originPoint * num1,
                                segment.controlPoint0 * num1,
                                segment.controlPoint1 * num1,
                                segment.point * num1,
                                EdgeColor.Black
                            )
                        );
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        var left = 0.0;
        var bottom = 0.0;
        var right = 0.0;
        var top = 0.0;
        shape.GetBounds(ref left, ref bottom, ref right, ref top);

        var width = right - left;
        var height = top - bottom;

        var num4 = -left;
        var num5 = -(top - height);
        var num6 = width + pixelRange;
        var num7 = height + pixelRange;

        var regionSize = region.size;
        if (rotated)
        {
            regionSize = regionSize.yx;
        }

        var scaleXY = MathX.Min(regionSize.x / num6, regionSize.y / num7);

        var offsetX = num4 + (pixelRange * 0.5);
        var offsetY = num5 + (pixelRange * 0.5);

        MSDF.EdgeColoringSimple(shape, 3.0);
        var list = Pool.BorrowRawValueList<float3>();
        var regionCeiling = MathX.CeilToInt(in regionSize);
        var area = regionCeiling.x * regionCeiling.y;
        if (list.Capacity < area)
        {
            list.Capacity = area;
        }

        shape.Normalize();
        shape.InverseYAxis = true;

        var workingBitmap = new Pixmap<Color3>(regionCeiling.x, regionCeiling.y);

        var scale = new Vector2((float)scaleXY);
        var translate = new Vector2((float)offsetX, (float)offsetY);

        MSDF.GenerateMSDF(workingBitmap, shape, pixelRange, scale, translate);

        // auto fix shapes with reversed winding
        var origin = new Vector2(-100000, -100000);
        var minDistance = shape.Contours
            .SelectMany(contour => contour.Edges)
            .Select(edge => edge.GetSignedDistance(origin, out _))
            .Min(_distanceComparer);

        if (minDistance.Distance < 0)
        {
            // equivalent to invertColor<3>(msdf)
            for (var i = 0; i < workingBitmap.Data.Length; i++)
            {
                workingBitmap.Data[i].R = 1.0f - workingBitmap.Data[i].R;
                workingBitmap.Data[i].G = 1.0f - workingBitmap.Data[i].G;
                workingBitmap.Data[i].B = 1.0f - workingBitmap.Data[i].B;
            }
        }

        // TODO: distanceSignCorrection(msdf, *shape, scale, translate, FILL_NONZERO)
        // TODO: msdfErrorCorrection(msdf, edgeThreshold / (scale*range)
        var regionOffsetX = MathX.RoundToInt(region.x);
        var regionOffsetY = MathX.RoundToInt(region.y);
        var num12 = MathX.CeilToInt(region.width);
        var num13 = MathX.CeilToInt(region.height);

        for (var y = 0; y < num13; ++y)
        {
            for (var x = 0; x < num12; ++x)
            {
                var index = shape.InverseYAxis
                    ? workingBitmap.GetIndex(y, x)
                    : workingBitmap.GetIndex(x, y);

                var rgb = workingBitmap.Data[index];
                bitmap.SetPixel(regionOffsetX + x, regionOffsetY + y, new color(rgb.R, rgb.G, rgb.B));
            }
        }

        __result = true;
        return false;
    }
}
