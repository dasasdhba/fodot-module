using System.Threading.Tasks;
using Godot;
using Moon.Utils;

namespace Moon.Component;

[GlobalClass]
public partial class Node2DAlpha : Node2D
{
    [ExportCategory("Node2DAlpha")]
    [Export]
    public bool Showing { get; set; } = true;

    [Export]
    public float Alpha { get; set; } = 1f;

    [Export]
    public float AlphaTime { get; set; } = 0.2f;

    [Signal]
    public delegate void AppearedEventHandler();

    [Signal]
    public delegate void DisappearedEventHandler();

    public void Appear() => Showing = true;

    public void Disappear() => Showing = false;

    public void QuickShow()
    {
        Showing = true;
        Alpha = 1f;
        Modulate = Modulate with { A = Alpha }; 
    }

    public void QuickHide()
    {
        Showing = false;
        Alpha = 0f;
        Modulate = Modulate with { A = Alpha }; 
    }

    public async Task AppearAsync()
    {
        if (Showing) return;
        Showing = true;
        await ToSignalAppeared();
    }
    
    public async Task DisappearAsync()
    {
        if (!Showing) return;
        Showing = false;
        await ToSignalDisappeared();
    }

    public Node2DAlpha() :base()
    {
        TreeEntered += () =>
        {
            Modulate = Modulate with { A = Alpha }; 
        };
        
        Ready += () =>
        {
            this.AddPhysicsProcess(delta =>
            {
                if (Showing && Alpha < 1f)
                {
                    Alpha += (float)(1f / AlphaTime * delta);
                    if (Alpha >= 1f)
                    {
                        Alpha = 1f;
                        EmitSignal(SignalName.Appeared);
                    }
                }
                if (!Showing && Alpha > 0f)
                {
                    Alpha -= (float)(1f / AlphaTime * delta);
                    if (Alpha <= 0f)
                    {
                        Alpha = 0f;
                        EmitSignal(SignalName.Disappeared);
                    }
                }

                Modulate = Modulate with { A = Alpha }; 
            });
        };
    }
}