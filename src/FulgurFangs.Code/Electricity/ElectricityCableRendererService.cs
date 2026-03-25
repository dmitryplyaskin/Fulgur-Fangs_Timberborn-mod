using System.Collections.Generic;
using System.Linq;
using Timberborn.TickSystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityCableRendererService : ITickableSingleton
{
    private readonly Dictionary<ElectricityCableEdgeKey, CablePair> _cablePairs = new();
    private readonly ElectricityService _electricityService;
    private readonly Material _cableMaterial;
    private readonly GameObject _rootObject;

    public ElectricityCableRendererService(ElectricityService electricityService)
    {
        _electricityService = electricityService;
        _rootObject = new GameObject("FulgurFangs.ElectricityCables")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        UnityEngine.Object.DontDestroyOnLoad(_rootObject);
        _cableMaterial = ElectricityCableVisuals.CreateCableMaterial();
        _electricityService.ConnectionsChanged += SyncConnections;
        SyncConnections(_electricityService.CurrentConnections);
    }

    public void Tick()
    {
    }

    private void SyncConnections(IReadOnlyList<ElectricityCableConnectionSnapshot> connections)
    {
        HashSet<ElectricityCableEdgeKey> activeKeys = new();
        foreach (ElectricityCableConnectionSnapshot connection in connections)
        {
            ElectricityCableEdgeKey key = new(connection.FirstNodeId, connection.SecondNodeId);
            activeKeys.Add(key);

            if (!_cablePairs.TryGetValue(key, out CablePair? cablePair) || cablePair == null || !cablePair.IsValid)
            {
                cablePair = CreateCablePair(key);
                _cablePairs[key] = cablePair;
            }

            cablePair.Update(connection);
        }

        ElectricityCableEdgeKey[] staleKeys = _cablePairs.Keys
            .Where(key => !activeKeys.Contains(key))
            .ToArray();
        foreach (ElectricityCableEdgeKey staleKey in staleKeys)
        {
            _cablePairs[staleKey].Destroy();
            _cablePairs.Remove(staleKey);
        }
    }

    private CablePair CreateCablePair(ElectricityCableEdgeKey key)
    {
        GameObject cableRoot = new($"ElectricityCable.{key.FirstNodeId}.{key.SecondNodeId}")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        cableRoot.transform.SetParent(_rootObject.transform, false);

        GameObject firstCable = new("CableA") { hideFlags = HideFlags.HideAndDontSave };
        firstCable.transform.SetParent(cableRoot.transform, false);
        GameObject secondCable = new("CableB") { hideFlags = HideFlags.HideAndDontSave };
        secondCable.transform.SetParent(cableRoot.transform, false);

        return new CablePair(
            cableRoot,
            ElectricityCableVisuals.CreateLineRenderer(firstCable, _cableMaterial, ElectricityCableVisuals.DefaultWidth),
            ElectricityCableVisuals.CreateLineRenderer(secondCable, _cableMaterial, ElectricityCableVisuals.DefaultWidth));
    }

    public void HighlightConnection(ElectricityPoleComponent first, ElectricityPoleComponent second, Color color)
    {
        if (_cablePairs.TryGetValue(new ElectricityCableEdgeKey(first.InstanceId, second.InstanceId), out CablePair? cablePair) &&
            cablePair != null)
        {
            cablePair.SetVisuals(color, ElectricityCableVisuals.HighlightWidth);
        }
    }

    public void UnhighlightConnection(ElectricityPoleComponent first, ElectricityPoleComponent second)
    {
        if (_cablePairs.TryGetValue(new ElectricityCableEdgeKey(first.InstanceId, second.InstanceId), out CablePair? cablePair) &&
            cablePair != null)
        {
            cablePair.SetVisuals(ElectricityCableVisuals.CableColor, ElectricityCableVisuals.DefaultWidth);
        }
    }

    private readonly struct ElectricityCableEdgeKey
    {
        public ElectricityCableEdgeKey(int leftNodeId, int rightNodeId)
        {
            FirstNodeId = Mathf.Min(leftNodeId, rightNodeId);
            SecondNodeId = Mathf.Max(leftNodeId, rightNodeId);
        }

        public int FirstNodeId { get; }

        public int SecondNodeId { get; }

        public bool Equals(ElectricityCableEdgeKey other)
        {
            return FirstNodeId == other.FirstNodeId && SecondNodeId == other.SecondNodeId;
        }

        public override bool Equals(object? obj) => obj is ElectricityCableEdgeKey other && Equals(other);

        public override int GetHashCode() => System.HashCode.Combine(FirstNodeId, SecondNodeId);
    }

    private sealed class CablePair
    {
        private readonly GameObject _rootObject;
        private readonly LineRenderer _firstCable;
        private readonly LineRenderer _secondCable;

        public CablePair(GameObject rootObject, LineRenderer firstCable, LineRenderer secondCable)
        {
            _rootObject = rootObject;
            _firstCable = firstCable;
            _secondCable = secondCable;
        }

        public bool IsValid => _rootObject != null && _firstCable != null && _secondCable != null;

        public void Update(ElectricityCableConnectionSnapshot connection)
        {
            ElectricityCableVisuals.UpdateParallelCablePair(
                _firstCable,
                _secondCable,
                connection.FirstAnchorWorldPosition,
                connection.SecondAnchorWorldPosition);
        }

        public void SetVisuals(Color color, float width)
        {
            _firstCable.widthMultiplier = width;
            _secondCable.widthMultiplier = width;
            ElectricityCableVisuals.ApplyColor(_firstCable, color);
            ElectricityCableVisuals.ApplyColor(_secondCable, color);
        }

        public void Destroy()
        {
            if (_rootObject != null)
            {
                UnityEngine.Object.Destroy(_rootObject);
            }
        }
    }
}
