using Godot;
using Moon.Utils;

namespace Moon.Component;

public partial class Node2DShake : Node2D
{
    [ExportCategory("Node2DShake")]
    [Export]
    public bool Disabled { get; set; }
    
    [Export]
    public double Frequency { get ;set; } = 0.02d;
    
    [Export]
    public Vector2 Amp { get ;set; } = new(1f, 1f);
    
    public Vector2 Origin { get ;set; }
    
    public void Activate() => Disabled = false;
    public void Disable() => Disabled = true;
    
    public Node2DShake() : base()
    {
         TreeEntered += () =>
         {
             Origin = Position;
         };
         
         Ready += () =>
         {
             this.ActionRepeatPhysics(Frequency, () =>
             {
                 if (Disabled)
                 {
                     Position = Origin;
                     return;
                 }
        
                 var offset = new Vector2(
                     Mathe.RandfRange(-Amp.X, Amp.X), 
                     Mathe.RandfRange(-Amp.Y, Amp.Y));
                 Position = Origin + offset; 
             });
         };
    }
}