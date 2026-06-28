using Godot;
using Moon.Library;

namespace Moon.Component;

[GlobalClass]
public abstract partial class NodeToggle : Node
{
    [ExportGroup("NodeToggle", "Editor")]
    [Export]
    public bool EditorFlag { get; set; } = true;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public double EditorValue { get; set; } = 1d;

    [Export(PropertyHint.Range, "0,3,0.01,or_greater,suffix:s")]
    public double EditorTime { get; set; } = 0.2d;
    
    [Export]
    public bool EditorPaused { get; set; } = false;
    
    [Export]
    public bool EditorPhysics { get; set; } = true;

    [Signal]
    public delegate void FullyOnEventHandler();

    [Signal]
    public delegate void FullyOffEventHandler();
    
    public SmoothToggle Toggle { get; set; }

    public bool Flag
    {
        get => Toggle?.Flag ?? false;
        set => Toggle?.Flag = value;
    }
    
    public double Value
    {
        get => Toggle?.Value ?? 0d;
        set => Toggle?.Value = value;
    }

    public double Time
    {
        get => Toggle?.Time ?? 0d;
        set => Toggle?.Time = value;
    }

    public bool Paused
    {
        get => Toggle?.Paused ?? false;
        set => Toggle?.Paused = value;
    }
    
    public bool IsFullyOn() => Toggle.IsFullyOn();
    public bool IsFullyOff() => Toggle.IsFullyOff();
    public void QuickOn() => Toggle.QuickOn();
    public void QuickOff() => Toggle.QuickOff();
    
    public abstract void OnValueUpdated(double v);
}