using Fodot.Injection;
using Godot;

namespace Moon.Class;

[GlobalClass, ChildOf("CollisionObject2D")]
public partial class MoonBlock2D : ParentComponent
{
    [Export]
    public Vector2[] InvalidSnaps { get ;set; } = [];
}
