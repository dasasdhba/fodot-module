namespace Moon.View

open Fodot.Core
open Godot
open Moon

type ViewShake =
    {
        Radius : Vector2
        LimitGuards : bool
        Frequency : float
        Time : float
    }
    
    static member New (amp, ?guards, ?freq, ?time) =
        {
            Radius = amp
            LimitGuards = defaultArg guards true
            Frequency = defaultArg freq 0.5
            Time = defaultArg time 0.5
        }
    
    static member New (amp, ?guards, ?freq, ?time) =
        ViewShake.New (Vector2(amp, amp), ?guards = guards, ?freq = freq, ?time = time)
    
    member private this.CreateTransformer offset (view: View2D)=
        if this.LimitGuards |> not then
            View2DTransformer.FromOffset offset
        else
        
        let target = view.CurrentPosition + offset
        let limits = view.GetLimitedPosition target
        let mutable guards = offset
        
        if limits.X < target.X then
            guards.X <- min 0f guards.X
        elif limits.X > target.X then
            guards.X <- max 0f guards.X
        
        if limits.Y < target.Y then
            guards.Y <- min 0f guards.Y
        elif limits.Y > target.Y then
            guards.Y <- max 0f guards.Y
        
        View2DTransformer.FromOffset guards
        
    member this.Apply (node : Node) =
        node
        |> Node.addViewTransformerBy (fun v ->
            let mutable timer = 0.0
            let mutable freq = 0.0
            let mutable offset = Vector2.Zero
            Delta (fun delta ->
                freq <- freq + delta
                if freq >= this.Frequency then
                    freq <- freq - this.Frequency
                    offset <- this.Radius.Randomize()
                
                if this.Time > 0.0 &&
                   (
                       timer <- timer + delta
                       timer >= this.Time
                   )
                then
                    None
                else
                    this.CreateTransformer offset v |> Some
            )
        )