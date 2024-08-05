using Editor;

namespace Sandbox.Events.Editor;

public sealed class TransitionItem : GraphicsItem
{
	public StateItem Source { get; }
	public StateItem Target { get; }

	public TransitionItem( StateItem source, StateItem target )
		: base( null )
	{
		Source = source;
		Target = target;

		ZIndex = 2;

		Source.PositionChanged += OnStatePositionChanged;
		Target.PositionChanged += OnStatePositionChanged;

		Layout();
	}

	protected override void OnDestroy()
	{
		Source.PositionChanged -= OnStatePositionChanged;
		Target.PositionChanged -= OnStatePositionChanged;
	}

	private void OnStatePositionChanged()
	{
		Layout();
	}

	protected override void OnPaint()
	{
		var sourceCenter = FromScene( Source.Center );
		var targetCenter = FromScene( Target.Center );

		var tangent = (targetCenter - sourceCenter).Normal;

		var start = sourceCenter + tangent * Source.Radius;
		var end = targetCenter - tangent * Target.Radius;

		var color = Color.White.Darken( 0.125f );

		Paint.SetPen( color, 4f );
		Paint.DrawLine( start + tangent * 2f, end - tangent * 16f );

		Paint.ClearPen();
		Paint.SetBrush( color );
		Paint.DrawArrow( end - tangent * 16f, end, 12f );
	}

	public void Layout()
	{
		var rect = Rect.FromPoints( Source.Center, Target.Center ).Grow( 16f );

		Position = rect.Position;
		Size = rect.Size;

		Update();
	}
}
