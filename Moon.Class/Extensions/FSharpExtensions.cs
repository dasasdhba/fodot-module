using System;
using FSharp;
using Godot;

namespace Moon.CSharp;

public static class FSharpExtensions
{
    public static void WhenReady(this Node node, Action action)
    {
        Module.Node.whenReady(action.AsFSharpFunc(), node);
    }

    // action

    public static Guid ActionRepeat(this Node node, double interval, Action action, bool physics = false)
    {
        return Module.Action.repeat(interval, physics, action.AsFSharpFunc(), node);
    }

    public static Guid ActionRepeatIdle(this Node node, double interval, Action action)
    {
        return node.ActionRepeat(interval, action, false);
    }
    
    public static Guid ActionRepeatPhysics(this Node node, double interval, Action action)
    {
        return node.ActionRepeat(interval, action, true);
    }

    public static Guid ActionDelay(this Node node, double interval, Action action, bool physics = false)
    {
        return Module.Action.delay(interval, physics, action.AsFSharpFunc(), node);
    }
    
    public static Guid ActionDelayIdle(this Node node, double interval, Action action)
    {
        return node.ActionDelay(interval, action, false);
    }
    
    public static Guid ActionDelayPhysics(this Node node, double interval, Action action)
    {
        return node.ActionDelay(interval, action, true);
    }
}