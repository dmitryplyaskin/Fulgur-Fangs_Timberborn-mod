using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityTowerPreview : BaseComponent, IAwakableComponent, IPostPlacementChangeListener, IPreviewSelectionListener
{
    private readonly ElectricityConnectionCandidates _electricityConnectionCandidates;
    private ElectricityPoleComponent? _electricityPoleComponent;
    private bool _isSelected;

    public ElectricityTowerPreview(ElectricityConnectionCandidates electricityConnectionCandidates)
    {
        _electricityConnectionCandidates = electricityConnectionCandidates;
    }

    public void Awake()
    {
        _electricityPoleComponent = GetComponent<ElectricityPoleComponent>();
    }

    public void OnPostPlacementChanged()
    {
        if (_isSelected)
        {
            _electricityConnectionCandidates.UpdateCandidates();
        }
    }

    public void OnPreviewSelect()
    {
        if (_isSelected || _electricityPoleComponent == null || _electricityPoleComponent.MaxConnections <= 0 || _electricityPoleComponent.MaxDistance <= 0f)
        {
            return;
        }

        _electricityConnectionCandidates.EnableAndDrawMarkers(_electricityPoleComponent);
        _isSelected = true;
    }

    public void OnPreviewUnselect()
    {
        _electricityConnectionCandidates.Disable();
        _isSelected = false;
    }
}
