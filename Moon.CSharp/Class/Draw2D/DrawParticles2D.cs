using System;
using System.Collections.Generic;
using Godot;
using Moon.CSharp;

namespace Moon.Class;

public abstract partial class DrawParticles2D : DrawProcess2D
{
    [ExportCategory("DrawParticles2D")]
    [Export]
    public bool Emitting { get; set; } = false;
    
    [Export(PropertyHint.Range, "0.001,4096,or_greater")]
    public double Interval { get ;set; } = 0.02d;
    
    /// <summary>
    /// Yield particle process calls here.
    /// </summary>
    protected abstract IEnumerable<Func<double, bool>> ParticleSetup();

    public DrawParticles2D() : base()
    {
        Ready += () =>
        {
            this.ActionRepeat(Interval, () =>
            {
                if (Emitting)
                {
                    foreach (var f in ParticleSetup())
                        AddDrawProcess(f);
                }
            },  ProcessCallback == Draw2DProcessCallback.Physics);
        };
    }
}