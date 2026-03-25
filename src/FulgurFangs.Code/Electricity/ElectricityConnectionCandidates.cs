using System.Collections.Generic;
using Timberborn.AssetSystem;
using Timberborn.Navigation;
using Timberborn.SelectionSystem;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityConnectionCandidates : ILoadableSingleton, IUpdatableSingleton, ISingletonPreviewNavMeshListener, ISingletonInstantNavMeshListener
{
    private static readonly Color OriginColor = new(0.3f, 0.85f, 0.82f, 1f);
    private readonly ElectricityConnectionService _electricityConnectionService;
    private readonly IAssetLoader _assetLoader;
    private readonly Highlighter _highlighter;
    private readonly List<ElectricityPoleComponent> _candidates = new();
    private readonly List<GameObject> _markers = new();
    private GameObject? _markerPrefab;
    private ElectricityPoleComponent? _origin;
    private bool _enabled;
    private bool _shouldUpdateCandidates;
    private bool _drawMarkers;

    public ElectricityConnectionCandidates(
        ElectricityConnectionService electricityConnectionService,
        IAssetLoader assetLoader,
        Highlighter highlighter)
    {
        _electricityConnectionService = electricityConnectionService;
        _assetLoader = assetLoader;
        _highlighter = highlighter;
    }

    public void Load()
    {
        _markerPrefab = _assetLoader.Load<GameObject>("Markers/ZiplineMarker");
    }

    public void EnableAndDrawMarkers(ElectricityPoleComponent origin)
    {
        EnableInternal(origin, true);
    }

    public void Enable(ElectricityPoleComponent origin)
    {
        EnableInternal(origin, false);
    }

    public void Disable()
    {
        _highlighter.UnhighlightAllPrimary();
        _origin = null;
        ClearCandidates();
        _enabled = false;
        _shouldUpdateCandidates = false;
    }

    public bool Contains(ElectricityPoleComponent? pole)
    {
        return pole != null && _candidates.Contains(pole);
    }

    public void UpdateSingleton()
    {
        if (_enabled && _shouldUpdateCandidates)
        {
            UpdateCandidates();
            _shouldUpdateCandidates = false;
        }
    }

    public void OnInstantNavMeshUpdated(NavMeshUpdate navMeshUpdate)
    {
        _shouldUpdateCandidates = true;
    }

    public void OnPreviewNavMeshUpdated(NavMeshUpdate navMeshUpdate)
    {
        _shouldUpdateCandidates = true;
    }

    public void UpdateCandidates()
    {
        ClearCandidates();
        AddCandidates();
    }

    private void EnableInternal(ElectricityPoleComponent origin, bool drawMarkers)
    {
        _origin = origin;
        _enabled = true;
        _drawMarkers = drawMarkers;
        _highlighter.HighlightPrimary(origin, OriginColor);
        UpdateCandidates();
        _shouldUpdateCandidates = false;
    }

    private void ClearCandidates()
    {
        _candidates.Clear();
        foreach (GameObject marker in _markers)
        {
            if (marker != null)
            {
                UnityEngine.Object.Destroy(marker);
            }
        }

        _markers.Clear();
    }

    private void AddCandidates()
    {
        if (_origin == null)
        {
            return;
        }

        foreach (ElectricityPoleComponent pole in _electricityConnectionService.RegisteredPoles)
        {
            if (!_electricityConnectionService.CanBeCandidate(_origin, pole))
            {
                continue;
            }

            _candidates.Add(pole);
            if (_drawMarkers)
            {
                CreateMarker(pole);
            }
        }
    }

    private void CreateMarker(ElectricityPoleComponent pole)
    {
        if (_markerPrefab == null || _origin == null)
        {
            return;
        }

        Vector3 markerPosition = pole.CableAnchorWorldPosition;
        Vector3 forward = markerPosition - _origin.CableAnchorWorldPosition;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
        GameObject marker = UnityEngine.Object.Instantiate(_markerPrefab, markerPosition + new Vector3(0f, 0.12f, 0f), rotation);
        _markers.Add(marker);
    }
}
