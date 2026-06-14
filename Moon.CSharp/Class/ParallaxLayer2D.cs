using Fodot.CSharp;
using Godot;
using Godot.Collections;

namespace Moon.Class;

[Tool]
#pragma warning disable CS0618 // Type or member is obsolete
public partial class ParallaxLayer2D : ParallaxLayer
#pragma warning restore CS0618 // Type or member is obsolete
{
    [Export]
    public Vector2 AutoScroll { get ;set; }

    public ParallaxLayer2D() : base()
    {
    #if DEBUG
        if (Engine.IsEditorHint()) return;
    #endif    
    
        Ready += () => this.AddPhysicsProcess(Process);
    }

    private void Process(double delta)
    {
        var offset = MotionOffset;

        if (MotionMirroring.X > 0f)
        {
            offset.X += (float)(AutoScroll.X * delta);
            offset.X = Mathf.Wrap(offset.X, 0f, MotionMirroring.X);
        }

        if (MotionMirroring.Y > 0f)
        {
            offset.Y += (float)(AutoScroll.Y * delta);
            offset.Y = Mathf.Wrap(offset.Y, 0f, MotionMirroring.Y);
        }
        
        MotionOffset = offset;
    }

#if DEBUG
    public override void _ValidateProperty(Dictionary property)
    {
        // disable transform
        var name = property["name"].AsStringName();
        
        if (
            name == Node2D.PropertyName.Position ||
            name == Node2D.PropertyName.Rotation ||
            name == Node2D.PropertyName.Scale ||
            name == Node2D.PropertyName.Skew
        )
        {
            property["usage"] = (uint)PropertyUsageFlags.None;
        }
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            Transform = new(0f, Vector2.Zero);
        }
    }
#endif
}