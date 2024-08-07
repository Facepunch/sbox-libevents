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

		ZIndex = -10;

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

	private enum TransitionKind
	{
		Default,
		Condition,
		Event
	}

	private (TransitionKind Kind, string? Icon, string? Title, int? Priority, float? Weight) GetLabelParts()
	{
		switch ( Transition )
		{
			case StateComponent state:
				return state.DefaultDuration > 0f
					? (TransitionKind.Event, "timer", $"After {FormatDuration( state.DefaultDuration )}", null, null)
					: (TransitionKind.Default, null, null, null, null);

			case ImmediateTransition immediate:
				return immediate.Condition.TryGetActionGraphImplementation( out var graph, out _ )
					? (TransitionKind.Condition, graph.Icon ?? "filter_alt", graph.Title, immediate.Priority, immediate.Weight)
					: (TransitionKind.Default, null, null, immediate.Priority, immediate.Weight);

			case IGameEventTransition eventTransition:
				var type = eventTransition.GameEventType;
				return (TransitionKind.Event, type.Icon, type.Title, null, null);

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
		Paint.DrawLine( start + tangent * 12f, end - tangent * 16f );

		Paint.ClearPen();
		Paint.SetBrush( color );
		Paint.DrawArrow( end - tangent * 16f, end, 12f );

		var (kind, icon, title, priority, weight) = GetLabelParts();

		var mid = (start + end) * 0.5f;
		var width = (end - start).Length;

		Paint.Translate( mid );
		Paint.Rotate( MathF.Atan2( tangent.y, tangent.x ) * 180f / MathF.PI );

		switch ( kind )
		{
			case TransitionKind.Default:
				Paint.ClearBrush();
				Paint.SetPen( color, 4f );
				Paint.DrawCircle( new Rect( -width * 0.5f - 10f, -10f, 20f, 20f ) );
				break;

			case TransitionKind.Condition:
				Paint.DrawCircle( new Rect( -width * 0.5f - 11f, -11f, 22f, 22f ) );
				break;

			case TransitionKind.Event:
				Paint.DrawRect( new Rect( -width * 0.5f - 10f, -8f, 20f, 16f ) );
				break;
		}

		if ( tangent.x < 0f )
		{
			Paint.Rotate( 180f );
		}

		Paint.ClearBrush();
		Paint.SetPen( color );
		Paint.SetFont( "roboto", 12f );

		var rect = new Rect( -width * 0.5f + 16f, -20f, width - 32f, 16f );

		const TextFlag textFlags = TextFlag.CenterBottom | TextFlag.SingleLine;

		if ( icon is not null )
		{
			rect = rect.Shrink( 24f, 0f, 0f, 0f );

			var textRect = Paint.MeasureText( rect, title ?? "", textFlags );

			Paint.DrawIcon( new Rect( textRect.Left - 24f, rect.Top, 16f, 16f ), icon, 16f );
		}

		if ( title is not null )
		{
			Paint.DrawText( rect, title, textFlags );
		}
	}

	public void Layout()
	{
		var rect = Rect.FromPoints( Source.Center, Target.Center ).Grow( 64f );

		Position = rect.Position;
		Size = rect.Size;

		Update();
	}
}
