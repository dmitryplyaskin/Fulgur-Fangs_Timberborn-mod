using Timberborn.ConstructionMode;
using Timberborn.InputSystem;
using Timberborn.Localization;
using Timberborn.SelectionSystem;
using Timberborn.ToolSystem;
using Timberborn.ToolSystemUI;
using Timberborn.UISound;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityConnectionAddingTool : ITool, IToolDescriptor, IInputProcessor, IConstructionModeEnabler
{
    private const string DescriptionLocKey = "Electricity.Connection.Tool.Description";
    private const string CursorKey = "PickObjectCursor";

    private readonly InputService _inputService;
    private readonly SelectableObjectRaycaster _selectableObjectRaycaster;
    private readonly EntitySelectionService _entitySelectionService;
    private readonly ToolService _toolService;
    private readonly ElectricityConnectionService _electricityConnectionService;
    private readonly ElectricityService _electricityService;
    private readonly CursorService _cursorService;
    private readonly ILoc _loc;
    private readonly ElectricityPreviewCableRenderer _electricityPreviewCableRenderer;
    private readonly UISoundController _uiSoundController;
    private readonly ElectricityConnectionCandidates _electricityConnectionCandidates;
    private ElectricityPoleComponent? _currentPole;

    public ElectricityConnectionAddingTool(
        InputService inputService,
        SelectableObjectRaycaster selectableObjectRaycaster,
        EntitySelectionService entitySelectionService,
        ToolService toolService,
        ElectricityConnectionService electricityConnectionService,
        ElectricityService electricityService,
        CursorService cursorService,
        ILoc loc,
        ElectricityPreviewCableRenderer electricityPreviewCableRenderer,
        UISoundController uiSoundController,
        ElectricityConnectionCandidates electricityConnectionCandidates)
    {
        _inputService = inputService;
        _selectableObjectRaycaster = selectableObjectRaycaster;
        _entitySelectionService = entitySelectionService;
        _toolService = toolService;
        _electricityConnectionService = electricityConnectionService;
        _electricityService = electricityService;
        _cursorService = cursorService;
        _loc = loc;
        _electricityPreviewCableRenderer = electricityPreviewCableRenderer;
        _uiSoundController = uiSoundController;
        _electricityConnectionCandidates = electricityConnectionCandidates;
    }

    public bool IsActiveFor(ElectricityPoleComponent? pole)
    {
        return pole != null &&
               ReferenceEquals(_toolService.ActiveTool, this) &&
               _currentPole != null &&
               _currentPole.InstanceId == pole.InstanceId;
    }

    public void SwitchTo(ElectricityPoleComponent pole)
    {
        _currentPole = pole;
    }

    public void Enter()
    {
        if (_currentPole == null)
        {
            _toolService.SwitchToDefaultTool();
            return;
        }

        _electricityConnectionCandidates.EnableAndDrawMarkers(_currentPole);
        _inputService.AddInputProcessor(this);
        _cursorService.SetCursor(CursorKey);
    }

    public void Exit()
    {
        ElectricityPoleComponent? currentPole = _currentPole;
        _electricityConnectionCandidates.Disable();
        _electricityPreviewCableRenderer.HidePreview();
        _inputService.RemoveInputProcessor(this);
        _cursorService.ResetCursor();
        if (currentPole != null && currentPole && currentPole.GameObject)
        {
            _entitySelectionService.Select(currentPole);
        }

        _currentPole = null;
    }

    public ToolDescription DescribeTool()
    {
        return new ToolDescription.Builder()
            .AddPrioritizedSection(_loc.T(DescriptionLocKey))
            .Build();
    }

    public bool ProcessInput()
    {
        if (_currentPole == null || !_currentPole || !_currentPole.GameObject || !_currentPole.IsReady)
        {
            _toolService.SwitchToDefaultTool();
            return true;
        }

        if (_inputService.Cancel || _inputService.UICancel)
        {
            _uiSoundController.PlayCancelSound();
            _toolService.SwitchToDefaultTool();
            return true;
        }

        ElectricityPoleComponent? targetPole = TryGetHoveredPole();
        if (_inputService.MainMouseButtonDown && !_inputService.MouseOverUI)
        {
            if (_electricityConnectionService.CanConnect(_currentPole, targetPole))
            {
                Connect(targetPole!);
                _uiSoundController.PlayClickSound();
                return true;
            }

            _uiSoundController.PlayCantDoSound();
            return true;
        }

        _electricityPreviewCableRenderer.DrawPreview(_currentPole, targetPole, _electricityConnectionCandidates.Contains(targetPole));
        return false;
    }

    private void Connect(ElectricityPoleComponent targetPole)
    {
        if (_currentPole == null || !_electricityConnectionService.TryConnect(_currentPole, targetPole))
        {
            return;
        }

        _electricityService.RefreshStateWithoutAdvancingTime();
        _toolService.SwitchToDefaultTool();
        _entitySelectionService.Select(targetPole);
        if (_electricityConnectionService.GetExplicitConnectionCount(targetPole) < targetPole.MaxConnections)
        {
            SwitchTo(targetPole);
            _toolService.SwitchTool(this);
        }
    }

    private ElectricityPoleComponent? TryGetHoveredPole()
    {
        if (!_selectableObjectRaycaster.TryHitSelectableObject(out SelectableObject? selectableObject) ||
            selectableObject == null)
        {
            return null;
        }

        ElectricityPoleComponent? pole = selectableObject.GetComponent<ElectricityPoleComponent>();
        return pole != null && pole.MaxConnections > 0 && pole.MaxDistance > 0f
            ? pole
            : null;
    }
}
