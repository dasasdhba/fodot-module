using Godot;

namespace Moon.Class;

[GlobalClass, Tool]
public partial class View2DRect : DebugFill
{
    public Rect2 GetRect() => new(GlobalPosition, Size);
}