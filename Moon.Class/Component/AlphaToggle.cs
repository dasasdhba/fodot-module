using Fodot.Injection;
using Godot;
using Godot.Warning;

namespace Moon.Component;

[GlobalClass, ChildOf("CanvasItem")]
public partial class AlphaToggle : NodeToggle
{
    private CanvasItem _parent;

    public override void OnValueUpdated(double v)
    {
        _parent ??= GetParentOrNull<CanvasItem>();
        _parent?.Modulate = _parent.Modulate with { A = (float)v };
    }
}
