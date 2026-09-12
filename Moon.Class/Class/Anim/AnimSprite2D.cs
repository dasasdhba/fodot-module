using Godot;

namespace Moon;

[GlobalClass]
public partial class AnimSprite2D : AnimatedSprite2D
{
    [Export]
    public bool AutoPlay { get; set; } = true;

    public AnimSprite2D() : base()
    {
        TreeEntered += () =>
        {
            ProcessCallback = AnimatedSprite2DProcessCallback.Physics;
            if (AutoPlay) Play();  
        };
    }
}
