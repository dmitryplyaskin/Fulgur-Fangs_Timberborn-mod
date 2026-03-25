using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace FulgurFangs.Code.Electricity;

public static class ElectricityCableVisuals
{
    public const int SegmentCount = 12;
    public const float CableSeparation = 0.14f;
    public const float DefaultWidth = 0.038f;
    public const float HighlightWidth = 0.052f;
    public static readonly Color CableColor = new(0.40f, 0.42f, 0.45f, 1f);

    private static readonly string[] ShaderNames =
    {
        "Standard",
        "Sprites/Default",
        "Unlit/Color",
        "UI/Default"
    };

    public static Material CreateCableMaterial()
    {
        Shader? shader = ShaderNames
            .Select(Shader.Find)
            .FirstOrDefault(static candidate => candidate != null);

        if (shader == null)
        {
            throw new MissingReferenceException("No supported shader was found for electricity cables.");
        }

        Material material = new(shader)
        {
            color = CableColor,
            hideFlags = HideFlags.HideAndDontSave
        };

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.18f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        return material;
    }

    public static LineRenderer CreateLineRenderer(GameObject rootObject, Material material, float width)
    {
        LineRenderer lineRenderer = rootObject.AddComponent<LineRenderer>();
        lineRenderer.hideFlags = HideFlags.HideAndDontSave;
        lineRenderer.sharedMaterial = material;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.positionCount = SegmentCount;
        lineRenderer.widthMultiplier = width;
        lineRenderer.generateLightingData = true;
        lineRenderer.numCornerVertices = 6;
        lineRenderer.numCapVertices = 4;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.enabled = true;
        ApplyColor(lineRenderer, CableColor);
        return lineRenderer;
    }

    public static void UpdateParallelCablePair(LineRenderer first, LineRenderer second, Vector3 start, Vector3 end)
    {
        Vector3 lateralOffset = GetLateralOffset(start, end);
        UpdateLineRenderer(first, start - lateralOffset, end - lateralOffset);
        UpdateLineRenderer(second, start + lateralOffset, end + lateralOffset);
    }

    public static void UpdateLineRenderer(LineRenderer lineRenderer, Vector3 start, Vector3 end)
    {
        Vector2 horizontal = new(end.x - start.x, end.z - start.z);
        float horizontalDistance = horizontal.magnitude;
        float sag = Mathf.Clamp(horizontalDistance * 0.045f, 0.08f, 0.65f);

        for (int index = 0; index < SegmentCount; index++)
        {
            float t = SegmentCount == 1 ? 0f : index / (float)(SegmentCount - 1);
            Vector3 point = Vector3.Lerp(start, end, t);
            float normalizedSag = 4f * t * (1f - t);
            point.y -= sag * normalizedSag;
            lineRenderer.SetPosition(index, point);
        }
    }

    public static void ApplyColor(LineRenderer lineRenderer, Color color)
    {
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    private static Vector3 GetLateralOffset(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        Vector3 lateral = Vector3.Cross(Vector3.up, direction.normalized);
        return lateral * (CableSeparation * 0.5f);
    }
}
