using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityConnectionService
{
    private readonly HashSet<ElectricityConnectionKey> _explicitConnections = new();
    private readonly Dictionary<int, ElectricityPoleComponent> _registeredPoles = new();
    private ElectricityPoleComponent? _pendingStartNode;

    public IEnumerable<ElectricityPoleComponent> RegisteredPoles => _registeredPoles.Values
        .Where(static pole => pole != null && pole && pole.GameObject && pole.Transform != null);

    public ElectricityPoleComponent? PendingStartNode
    {
        get
        {
            if (_pendingStartNode == null || !_pendingStartNode || !_pendingStartNode.GameObject)
            {
                _pendingStartNode = null;
            }

            return _pendingStartNode;
        }
    }

    public IReadOnlyCollection<ElectricityConnectionKey> ExplicitConnections => _explicitConnections;

    public bool HasPendingConnection => PendingStartNode != null;

    public void RegisterPole(ElectricityPoleComponent pole)
    {
        _registeredPoles[pole.InstanceId] = pole;
    }

    public void UnregisterPole(ElectricityPoleComponent? pole)
    {
        if (pole == null)
        {
            return;
        }

        _registeredPoles.Remove(pole.InstanceId);
        ClearConnections(pole);
    }

    public void BeginConnection(ElectricityPoleComponent? node)
    {
        _pendingStartNode = node != null && node.IsReady ? node : null;
    }

    public void CancelPendingConnection()
    {
        _pendingStartNode = null;
    }

    public bool AreConnected(ElectricityPoleComponent? first, ElectricityPoleComponent? second)
    {
        if (first == null || second == null || !first || !second)
        {
            return false;
        }

        return _explicitConnections.Contains(new ElectricityConnectionKey(first.InstanceId, second.InstanceId));
    }

    public bool HasExplicitConnections(ElectricityPoleComponent? node)
    {
        return node != null && _explicitConnections.Any(connection => connection.Contains(node.InstanceId));
    }

    public int GetExplicitConnectionCount(ElectricityPoleComponent? node)
    {
        return node == null
            ? 0
            : _explicitConnections.Count(connection => connection.Contains(node.InstanceId));
    }

    public IReadOnlyList<ElectricityPoleComponent> GetExplicitConnectionTargets(ElectricityPoleComponent? node)
    {
        if (node == null)
        {
            return System.Array.Empty<ElectricityPoleComponent>();
        }

        return _explicitConnections
            .Where(connection => connection.Contains(node.InstanceId))
            .Select(connection => connection.GetOtherNodeId(node.InstanceId))
            .Where(otherNodeId => _registeredPoles.TryGetValue(otherNodeId, out ElectricityPoleComponent? target) && target != null && target.GameObject)
            .Select(otherNodeId => _registeredPoles[otherNodeId])
            .OrderBy(static target => target.InstanceId)
            .ToArray();
    }

    public void AddLoadedConnections(ElectricityPoleComponent? node, IEnumerable<ElectricityPoleComponent>? targets)
    {
        if (node == null || targets == null)
        {
            return;
        }

        foreach (ElectricityPoleComponent target in targets)
        {
            if (target == null || !target || !target.GameObject || node.InstanceId == target.InstanceId)
            {
                continue;
            }

            _explicitConnections.Add(new ElectricityConnectionKey(node.InstanceId, target.InstanceId));
        }
    }

    public void CleanupNodes(IReadOnlyCollection<ElectricityPoleComponent> nodes)
    {
        HashSet<int> validIds = nodes
            .Where(static node => node != null && node.IsReady)
            .Select(static node => node.InstanceId)
            .ToHashSet();

        int[] stalePoleIds = _registeredPoles
            .Where(static pair => pair.Value == null || !pair.Value || !pair.Value.GameObject || pair.Value.Transform == null)
            .Select(static pair => pair.Key)
            .ToArray();
        foreach (int stalePoleId in stalePoleIds)
        {
            _registeredPoles.Remove(stalePoleId);
        }

        _explicitConnections.RemoveWhere(connection =>
            !validIds.Contains(connection.FirstNodeId) ||
            !validIds.Contains(connection.SecondNodeId));

        if (PendingStartNode != null && !validIds.Contains(PendingStartNode.InstanceId))
        {
            _pendingStartNode = null;
        }
    }

    public bool CanConnect(ElectricityPoleComponent? first, ElectricityPoleComponent? second)
    {
        return CanBeCandidate(first, second) &&
               first != null &&
               second != null &&
               first.IsReady &&
               second.IsReady;
    }

    public bool CanBeCandidate(ElectricityPoleComponent? first, ElectricityPoleComponent? second)
    {
        if (first == null || second == null || !first || !second || !second.GameObject)
        {
            return false;
        }

        if (first.InstanceId == second.InstanceId || AreConnected(first, second))
        {
            return false;
        }

        if (first.MaxConnections <= 0 || second.MaxConnections <= 0)
        {
            return false;
        }

        if (!second.IsReady)
        {
            return false;
        }

        if ((first.IsReady && GetExplicitConnectionCount(first) >= first.MaxConnections) ||
            GetExplicitConnectionCount(second) >= second.MaxConnections)
        {
            return false;
        }

        float distance = Vector3.Distance(first.CableAnchorWorldPosition, second.CableAnchorWorldPosition);
        return distance <= Mathf.Min(first.MaxDistance, second.MaxDistance);
    }

    public bool TryConnect(ElectricityPoleComponent? first, ElectricityPoleComponent? second)
    {
        if (!CanConnect(first, second))
        {
            return false;
        }

        _explicitConnections.Add(new ElectricityConnectionKey(first!.InstanceId, second!.InstanceId));
        return true;
    }

    public bool TryConnectPendingTo(ElectricityPoleComponent? target)
    {
        ElectricityPoleComponent? pendingStartNode = PendingStartNode;
        if (!TryConnect(pendingStartNode, target))
        {
            return false;
        }

        _pendingStartNode = null;
        return true;
    }

    public bool DisconnectPendingFrom(ElectricityPoleComponent? target)
    {
        ElectricityPoleComponent? pendingStartNode = PendingStartNode;
        if (!Disconnect(pendingStartNode, target))
        {
            return false;
        }

        _pendingStartNode = null;
        return true;
    }

    public bool Disconnect(ElectricityPoleComponent? first, ElectricityPoleComponent? second)
    {
        if (first == null || second == null || !first || !second)
        {
            return false;
        }

        return _explicitConnections.Remove(new ElectricityConnectionKey(first.InstanceId, second.InstanceId));
    }

    public int ClearConnections(ElectricityPoleComponent? node)
    {
        if (node == null)
        {
            return 0;
        }

        ElectricityConnectionKey[] removedKeys = _explicitConnections
            .Where(connection => connection.Contains(node.InstanceId))
            .ToArray();

        foreach (ElectricityConnectionKey removedKey in removedKeys)
        {
            _explicitConnections.Remove(removedKey);
        }

        if (PendingStartNode?.InstanceId == node.InstanceId)
        {
            _pendingStartNode = null;
        }

        return removedKeys.Length;
    }
}

public readonly record struct ElectricityConnectionKey
{
    public ElectricityConnectionKey(int leftNodeId, int rightNodeId)
    {
        FirstNodeId = Mathf.Min(leftNodeId, rightNodeId);
        SecondNodeId = Mathf.Max(leftNodeId, rightNodeId);
    }

    public int FirstNodeId { get; }

    public int SecondNodeId { get; }

    public bool Contains(int nodeId) => FirstNodeId == nodeId || SecondNodeId == nodeId;

    public int GetOtherNodeId(int nodeId) => FirstNodeId == nodeId ? SecondNodeId : FirstNodeId;
}
