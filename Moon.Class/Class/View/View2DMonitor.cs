using Fodot.CSharp;
using Godot;
using Godot.Collections;

namespace Moon.Component;

[GlobalClass, Tool]
public partial class View2DMonitor : Marker2D
{
	[Export]
	public bool AutoInvoke { get ;set; } = false;

	public enum ViewArea { Current, All }
	
	[Export]
	public ViewArea Area { get; set; } = ViewArea.Current;
	
	public enum MonitorMode { Direction, Manual, Total }

	[ExportGroup("ViewSettings")]
	[Export]
	public MonitorMode Mode
	{
		get => _mode;
		set
		{
			_mode = value;
			NotifyPropertyListChanged();
		}    
	}
	
	private MonitorMode _mode;
	
	[Export]
	public Vector2 Direction { get ;set; } = Vector2.Down;
	
	[Export(PropertyHint.None, "suffix:px")]
	public float Range { get; set; } = 48f;
	
	[Export]
	public bool MonitorUp { get; set; } = true;
	
	[Export(PropertyHint.None, "suffix:px")]
	public float UpRange { get; set; } = 48f;
	
	[Export]
	public bool MonitorDown { get; set; } = true;
	
	[Export(PropertyHint.None, "suffix:px")]
	public float DownRange { get; set; } = 48f;
	
	[Export]
	public bool MonitorLeft { get; set; } = true;
	
	[Export(PropertyHint.None, "suffix:px")]
	public float LeftRange { get; set; } = 48f;
	
	[Export]
	public bool MonitorRight { get; set; } = true;
	
	[Export(PropertyHint.None, "suffix:px")]
	public float RightRange { get; set; } = 48f;
	
	[Signal]
	public delegate void InvokeInViewEventHandler();
	
	[Signal]
	public delegate void InvokeOutViewEventHandler();

#if DEBUG
	
	public override void _ValidateProperty(Dictionary property)
	{
		if (
			(Mode == MonitorMode.Direction && (string)property["name"] is
				"UpRange" or "DownRange" or "LeftRange" or "RightRange" or 
				"MonitorUp" or "MonitorDown" or "MonitorLeft" or "MonitorRight") ||
			(Mode == MonitorMode.Manual && (string)property["name"] is 
				"Direction" or "Range") ||
			(Mode == MonitorMode.Total && (string)property["name"] is
				"UpRange" or "DownRange" or "LeftRange" or "RightRange" or
				"MonitorUp" or "MonitorDown" or "MonitorLeft" or "MonitorRight" or  
				"Direction")
		)
		{
			property["usage"] = (uint)PropertyUsageFlags.ReadOnly;
		}
	}
	
#endif
	
	private bool IsInArea() => Area == ViewArea.Current ?
		this.IsInView(Range) : this.IsInViewRegion(Range);
	
	private bool IsInAreaDir() => Area == ViewArea.Current ?
		this.IsInViewDir(Direction, Range) :
		this.IsInViewRegionDir(Direction, Range);
		
	private bool IsInAreaTop() => Area == ViewArea.Current ?
		this.IsInViewTop(UpRange) :
		this.IsInViewRegionTop(UpRange);
		
	private bool IsInAreaBottom() => Area == ViewArea.Current ?
		this.IsInViewBottom(DownRange) :
		this.IsInViewRegionBottom(DownRange);
		
	private bool IsInAreaLeft() => Area == ViewArea.Current ?
		this.IsInViewLeft(LeftRange) :
		this.IsInViewRegionLeft(LeftRange);
	
	private bool IsInAreaRight() => Area == ViewArea.Current ?
		this.IsInViewRight(RightRange) :
		this.IsInViewRegionRight(RightRange);

	public bool IsInView()
	{
		if (Mode == MonitorMode.Total)
		{
			return IsInArea();
		}
	
		if (Mode == MonitorMode.Direction)
		{
			return IsInAreaDir();
		}

		return (!MonitorUp || IsInAreaTop())
			&& (!MonitorDown || IsInAreaBottom())
			&& (!MonitorLeft || IsInAreaLeft())
			&& (!MonitorRight || IsInAreaRight());
	}

	public void Invoke()
	{
		if (IsInView()) EmitSignalInvokeInView();
		else EmitSignalInvokeOutView();
	}

	public View2DMonitor() : base()
	{
	#if DEBUG
		if (Engine.IsEditorHint()) return;	
	#endif
	
		Ready += () =>
		{
			if (AutoInvoke) this.AddPhysicsProcess(Invoke);	
		};
	}
}