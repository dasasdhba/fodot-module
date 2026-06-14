using Godot;
using Moon.CSharp;

namespace Moon.Component;

[GlobalClass]
public partial class Shake2D : Node2D
{
    [Export]
    public bool Disabled { get; set; }
    
    [Export]
    public double Frequency { get ;set; } = 0.02d;
    
    [Export]
    public Vector2 Amp { get ;set; } = new(1f, 1f);
    
    public Vector2 Origin { get ;set; }
    
    public void Activate() => Disabled = false;
    public void Disable() => Disabled = true;
    
    public Shake2D() : base()
    {
         Ready += () =>
         {
             Origin = Position;
         
             this.ActionRepeatPhysics(Frequency, () =>
             {
                 if (Disabled)
                 {
                     Position = Origin;
                     return;
                 }
        
                 var offset = Amp.Randomize();
                 Position = Origin + offset; 
             });
         };
    }
}