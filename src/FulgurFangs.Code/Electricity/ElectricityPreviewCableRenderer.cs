using System.Linq;
using Timberborn.SelectionSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityPreviewCableRenderer
{
    private static readonly Color ValidPreviewColor = new(0.21f, 0.66f, 0.3f, 1f);
    private static readonly Color InvalidPreviewColor = new(0.78f, 0.2f, 0.2f, 1f);
    private static readonly string[] ShaderNames =
    {
        "Sprites/Default",
        "Unlit/Color",
        "UI/Default"
    };

    private readonly Highlighter _highlighter;
    private readonly LineRenderer _lineRenderer;
    private ElectricityPoleComponent? _highlightedTarget;

    public ElectricityPreviewCableRenderer(Highlighter highlighter)
    {
        _highlighter = highlighter;

        GameObject rootObject = new("FulgurFangs.ElectricityCablePreview")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        UnityEngine.Object.DontDestroyOnLoad(rootObject);
        _lineRenderer = CreateLineRenderer(rootObject);
        HidePreview();
    }

    public void DrawPreview(ElectricityPoleComponent? start, ElectricityPoleComponent? target, bool canConnect)
    {
        if (start == null || target == null || !start || !target || start.InstanceId == target.InstanceId)
        {
            HidePreview();
            return;
        }

        Color color = canConnect ? ValidPreviewColor : InvalidPreviewColor;
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;
        _lineRenderer.enabled = true;
        UpdateLineRenderer(_lineRenderer, start.CableAnchorWorldPosition, target.CableAnchorWorldPosition);

        if (_highlightedTarget != null && _highlightedTarget != target)
        {
            ClearTargetHighlight(_highlightedTarget);
        }

        _highlightedTarget = target;
        _highlighter.HighlightSecondary(target, color);
    }

    public void HidePreview()
    {
        _lineRenderer.enabled = false;
        if (_highlightedTarget != null)
        {
            ClearTargetHighlight(_highlightedTarget);
            _highlightedTarget = null;
        }
    }

    private void ClearTargetHighlight(ElectricityPoleComponent target)
    {
        _highlighter.UnhighlightSecondary(target);
        _highlighter.UnhighlightPrimary(target);
    }

    private static LineRenderer CreateLineRenderer(GameObject rootObject)
    {
        LineRenderer lineRenderer = rootObject.AddComponent<LineRenderer>();
        lineRenderer.hideFlags = HideFlags.HideAndDontSave;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.positionCount = 4;
        lineRenderer.widthMultiplier = 0.065f;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 2;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.sharedMaterial = CreateMaterial();
        return lineRenderer;
    }

    private static void UpdateLineRenderer(LineRenderer lineRenderer, Vector3 start, Vector3 end)
    {
        float horizontalDistance = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(end.x, end.z));
        float sag = Mathf.Clamp(horizontalDistance * 0.03f, 0.05f, 0.45f);

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, Vector3.Lerp(start, end, 0.33f) + Vector3.down * sag);
        lineRenderer.SetPosition(2, Vector3.Lerp(start, end, 0.66f) + Vector3.down * sag);
        lineRenderer.SetPosition(3, end);
    }

    private static Material CreateMaterial()
    {
        Shader? shader = ShaderNames
            .Select(Shader.Find)
            .FirstOrDefault(static candidate => candidate != null);
        if (shader == null)
        {
            throw new MissingReferenceException("No supported shader was found for electricity cable previews.");
        }

        return new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }
}
