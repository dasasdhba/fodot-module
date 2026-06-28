using Fodot.CSharp;
using Godot;
using Godot.Warning;

namespace Moon.Component;

[GlobalClass, ChildOf("CanvasItem")]
public partial class AlphaToggle : NodeToggle
{
    /// <summary>
    /// It's recommended to increase process priority to make this work.
    /// </summary>
    [Export]
    public bool HackVisibility { get ;set; }

    private CanvasItem _parent;

    public override void OnValueUpdated(double v)
    {
        if (!IsInstanceValid(_parent)) return;
        
        _parent.Modulate = _parent.Modulate with { A = (float)v };
    }

    public override void _EnterTree()
    {
        base._EnterTree();
    #if DEBUG
        if (Engine.IsEditorHint()) return;
    #endif
        
        _parent = GetParentOrNull<CanvasItem>();
    }

    public override void _Ready()
    {
        base._Ready();
    #if DEBUG
        if (Engine.IsEditorHint()) return;
    #endif
    
        if (HackVisibility) this.AddProcess(Hack, EditorPhysics);
    }

    private void Hack()
    {
        if (!IsInstanceValid(_parent)) return;
        
        Flag = _parent.Visible;
        _parent.Visible = true;
    }
}
