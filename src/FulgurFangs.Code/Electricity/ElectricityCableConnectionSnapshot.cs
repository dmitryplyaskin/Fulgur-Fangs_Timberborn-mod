using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public readonly record struct ElectricityCableConnectionSnapshot(
    int FirstNodeId,
    Vector3 FirstAnchorWorldPosition,
    int SecondNodeId,
    Vector3 SecondAnchorWorldPosition);
