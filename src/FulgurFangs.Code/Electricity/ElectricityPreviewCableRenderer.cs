using Timberborn.SelectionSystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityPreviewCableRenderer
{
    private static readonly Color ValidPreviewColor = new(0.21f, 0.66f, 0.3f, 1f);
    private static readonly Color InvalidPreviewColor = new(0.78f, 0.2f, 0.2f, 1f);

    private readonly Highlighter _highlighter;
    private readonly LineRenderer _firstLineRenderer;
    private readonly LineRenderer _secondLineRenderer;
    private ElectricityPoleComponent? _highlightedTarget;

    public ElectricityPreviewCableRenderer(Highlighter highlighter)
    {
        _highlighter = highlighter;

        GameObject rootObject = new("FulgurFangs.ElectricityCablePreview")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        UnityEngine.Object.DontDestroyOnLoad(rootObject);
        Material material = ElectricityCableVisuals.CreateCableMaterial();

        GameObject firstPreview = new("PreviewCableA") { hideFlags = HideFlags.HideAndDontSave };
        firstPreview.transform.SetParent(rootObject.transform, false);
        GameObject secondPreview = new("PreviewCableB") { hideFlags = HideFlags.HideAndDontSave };
        secondPreview.transform.SetParent(rootObject.transform, false);

        _firstLineRenderer = ElectricityCableVisuals.CreateLineRenderer(firstPreview, material, ElectricityCableVisuals.DefaultWidth);
        _secondLineRenderer = ElectricityCableVisuals.CreateLineRenderer(secondPreview, material, ElectricityCableVisuals.DefaultWidth);
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
        SetPreviewVisible(true);
        ElectricityCableVisuals.ApplyColor(_firstLineRenderer, color);
        ElectricityCableVisuals.ApplyColor(_secondLineRenderer, color);
        ElectricityCableVisuals.UpdateParallelCablePair(
            _firstLineRenderer,
            _secondLineRenderer,
            start.CableAnchorWorldPosition,
            target.CableAnchorWorldPosition);

        if (_highlightedTarget != null && _highlightedTarget != target)
        {
            ClearTargetHighlight(_highlightedTarget);
        }

        _highlightedTarget = target;
        _highlighter.HighlightSecondary(target, color);
    }

    public void HidePreview()
    {
        SetPreviewVisible(false);
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

    private void SetPreviewVisible(bool visible)
    {
        _firstLineRenderer.enabled = visible;
        _secondLineRenderer.enabled = visible;
    }
}
