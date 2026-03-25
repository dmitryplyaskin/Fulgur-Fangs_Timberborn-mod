using System.Collections.Generic;
using System.Linq;
using Timberborn.TickSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityCableRendererService : ITickableSingleton
{
    private static readonly Color CableColor = new(0.13f, 0.17f, 0.19f, 1f);
    private const float DefaultWidth = 0.06f;
    private const float HighlightWidth = 0.085f;
    private static readonly string[] ShaderNames =
    {
        "Sprites/Default",
        "Unlit/Color",
        "UI/Default"
    };

    private readonly Dictionary<ElectricityCableEdgeKey, LineRenderer> _lineRenderers = new();
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
        _cableMaterial = CreateCableMaterial();
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

            if (!_lineRenderers.TryGetValue(key, out LineRenderer? lineRenderer) || lineRenderer == null)
            {
                lineRenderer = CreateLineRenderer(key);
                _lineRenderers[key] = lineRenderer;
            }

            UpdateLineRenderer(lineRenderer, connection);
        }

        ElectricityCableEdgeKey[] staleKeys = _lineRenderers.Keys
            .Where(key => !activeKeys.Contains(key))
            .ToArray();
        foreach (ElectricityCableEdgeKey staleKey in staleKeys)
        {
            LineRenderer lineRenderer = _lineRenderers[staleKey];
            if (lineRenderer != null)
            {
                UnityEngine.Object.Destroy(lineRenderer.gameObject);
            }

            _lineRenderers.Remove(staleKey);
        }
    }

    private LineRenderer CreateLineRenderer(ElectricityCableEdgeKey key)
    {
        GameObject cableObject = new($"ElectricityCable.{key.FirstNodeId}.{key.SecondNodeId}")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        cableObject.transform.SetParent(_rootObject.transform, false);

        LineRenderer lineRenderer = cableObject.AddComponent<LineRenderer>();
        lineRenderer.hideFlags = HideFlags.HideAndDontSave;
        lineRenderer.sharedMaterial = _cableMaterial;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.positionCount = 4;
        lineRenderer.widthMultiplier = DefaultWidth;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 2;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        ApplyColor(lineRenderer, CableColor);
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.enabled = true;
        return lineRenderer;
    }

    public void HighlightConnection(ElectricityPoleComponent first, ElectricityPoleComponent second, Color color)
    {
        if (_lineRenderers.TryGetValue(new ElectricityCableEdgeKey(first.InstanceId, second.InstanceId), out LineRenderer? lineRenderer) &&
            lineRenderer != null)
        {
            lineRenderer.widthMultiplier = HighlightWidth;
            ApplyColor(lineRenderer, color);
        }
    }

    public void UnhighlightConnection(ElectricityPoleComponent first, ElectricityPoleComponent second)
    {
        if (_lineRenderers.TryGetValue(new ElectricityCableEdgeKey(first.InstanceId, second.InstanceId), out LineRenderer? lineRenderer) &&
            lineRenderer != null)
        {
            lineRenderer.widthMultiplier = DefaultWidth;
            ApplyColor(lineRenderer, CableColor);
        }
    }

    private static void UpdateLineRenderer(LineRenderer lineRenderer, ElectricityCableConnectionSnapshot connection)
    {
        Vector3 start = connection.FirstAnchorWorldPosition;
        Vector3 end = connection.SecondAnchorWorldPosition;
        float horizontalDistance = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(end.x, end.z));
        float sag = Mathf.Clamp(horizontalDistance * 0.03f, 0.05f, 0.45f);

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, Vector3.Lerp(start, end, 0.33f) + Vector3.down * sag);
        lineRenderer.SetPosition(2, Vector3.Lerp(start, end, 0.66f) + Vector3.down * sag);
        lineRenderer.SetPosition(3, end);
    }

    private static Material CreateCableMaterial()
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
        return material;
    }

    private static void ApplyColor(LineRenderer lineRenderer, Color color)
    {
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
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
}
