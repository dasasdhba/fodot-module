using Godot;
using Godot.Warning;

namespace Moon.Component;

[GlobalClass, ChildOf("CanvasItem")]
public partial class SpriteDir : Node
{
    /// <summary>
    /// The monitored moving node.
    /// </summary>
    [Export(PropertyHint.NodePathValidTypes, "CanvasItem")]
    public NodePath TrackingNode { get ;set; }
    
    [Export(PropertyHint.NodePathValidTypes, "Rotator")]
    public NodePath RotatorNode { get ;set; }
    
    [Export]
    public bool Flip { get ;set; }
    
    [Export]
    public bool Disabled { get ;set; }
}
