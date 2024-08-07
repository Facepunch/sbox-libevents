using System;
using Editor;
using Facepunch.ActionGraphs;
using Math = Facepunch.ActionGraphs.Nodes.Math;

namespace Sandbox.Events.Editor;

public sealed class TransitionItem : GraphicsItem
{
	public ITransition Transition { get; }
	public StateItem Source { get; }
	public StateItem Target { get; }

	public TransitionItem( ITransition transition, StateItem source, StateItem target )
		: base( null )
	{
		Transition = transition;

		Source = source;
		Target = target;

		Selectable = true;
		HoverEvents = true;

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

	public override bool Contains( Vector2 localPos )
	{
		if ( GetLocalStartEnd() is not var (start, end, tangent) )
		{
			return false;
		}

		var t = Vector2.Dot( localPos - start, tangent );

		if ( t < 0f || t > (end - start).Length ) return false;

		var s = Vector2.Dot( localPos - start, tangent.Perpendicular );

		return Math.Abs( s ) <= 8f;
	}

	private void OnStatePositionChanged()
	{
		Layout();
	}

	private (Vector2 Start, Vector2 End, Vector2 Tangent)? GetLocalStartEnd()
	{
		var sourceCenter = FromScene( Source.Center );
		var targetCenter = FromScene( Target.Center );

		if ( (targetCenter - sourceCenter).IsNearZeroLength )
		{
			return null;
		}

		var tangent = (targetCenter - sourceCenter).Normal;

		var start = sourceCenter + tangent * Source.Radius;
		var end = targetCenter - tangent * Target.Radius;

		return (start, end, tangent);
	}

	private string FormatDuration( float seconds )
	{
		var timeSpan = TimeSpan.FromSeconds( seconds );
		var result = "";

		if ( timeSpan.Hours > 0 )
		{
			result += $"{timeSpan.Hours}h";
		}

		if ( timeSpan.Minutes > 0 )
		{
			result += $"{timeSpan.Minutes}m";
		}

		if ( timeSpan.Seconds > 0 )
		{
			result += $"{timeSpan.Seconds}s";
		}

		if ( timeSpan.Milliseconds > 0 )
		{
			result += $"{timeSpan.Milliseconds}ms";
		}

		return result;
	}

	private (string? Icon, string? Condition, int? Priority, float? Weight) GetLabelParts()
	{
		switch ( Transition )
		{
			case StateComponent state:
				return state.DefaultDuration > 0f
					? ("timer", $"After {FormatDuration( state.DefaultDuration )}", null, null)
					: (null, null, null, null);

			case ImmediateTransition immediate:
				return immediate.Condition.TryGetActionGraphImplementation( out var graph, out _ )
					? (graph.Icon ?? "filter_alt", graph.Title, immediate.Priority, immediate.Weight)
					: (null, null, immediate.Priority, immediate.Weight);

			case IGameEventTransition eventTransition:
				var type = eventTransition.GameEventType;
				return (type.Icon, type.Title, null, null);

			default:
				throw new NotImplementedException();
		}
	}

	protected override void OnPaint()
	{
		if ( GetLocalStartEnd() is not var (start, end, tangent) )
		{
			return;
		}

		var color = Selected || Hovered
			? Color.White
			: Color.White.Darken( 0.125f );

		Paint.SetPen( color, Selected || Hovered ? 6f : 4f );
		Paint.DrawLine( start + tangent * 2f, end - tangent * 16f );

		Paint.ClearPen();
		Paint.SetBrush( color );
		Paint.DrawArrow( end - tangent * 16f, end, 12f );

		Paint.ClearBrush();
		Paint.SetPen( color );
		Paint.SetFont( "roboto", 12f );

		var (icon, condition, priority, weight) = GetLabelParts();

		if ( condition is null )
		{
			return;
		}

		var mid = (start + end) * 0.5f;
		var width = (end - start).Length;

		if ( tangent.x < 0f )
		{
			tangent = -tangent;
		}

		Paint.Translate( mid );
		Paint.Rotate( MathF.Atan2( tangent.y, tangent.x ) * 180f / MathF.PI );

		var rect = new Rect( -width * 0.5f + 16f, -20f, width - 32f, 16f );

		const TextFlag textFlags = TextFlag.CenterBottom | TextFlag.SingleLine;

		if ( icon is not null )
		{
			rect = rect.Shrink( 24f, 0f, 0f, 0f );

			var textRect = Paint.MeasureText( rect, condition, textFlags );

			Paint.DrawIcon( new Rect( textRect.Left - 24f, rect.Top, 16f, 16f ), icon, 16f );
		}

		Paint.DrawText( rect, condition, textFlags );
	}

	public void Layout()
	{
		var rect = Rect.FromPoints( Source.Center, Target.Center ).Grow( 64f );

		Position = rect.Position;
		Size = rect.Size;

		Update();
	}
}
