using System;
using Editor;
using Facepunch.ActionGraphs;
using Math = Facepunch.ActionGraphs.Nodes.Math;

namespace Sandbox.Events.Editor;

public sealed class TransitionItem : GraphicsItem
{
	public TransitionComponent? Transition { get; }
	public StateItem Source { get; }
	public StateItem? Target { get; set; }
	public Vector2 TargetPosition { get; set; }

	public TransitionItem( TransitionComponent? transition, StateItem source, StateItem? target )
		: base( null )
	{
		Transition = transition;

		Source = source;
		Target = target;

		Selectable = true;
		HoverEvents = true;

		ZIndex = -10;

		if ( Transition is not null )
		{
			Source.PositionChanged += OnStatePositionChanged;
			Target!.PositionChanged += OnStatePositionChanged;
		}

		Layout();
	}

	protected override void OnDestroy()
	{
		if ( Transition is null ) return;

		Source.PositionChanged -= OnStatePositionChanged;
		Target!.PositionChanged -= OnStatePositionChanged;
	}

	public override bool Contains( Vector2 localPos )
	{
		if ( Transition is null )
		{
			return false;
		}

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
		var targetCenter = Target is null ? FromScene( TargetPosition ) : FromScene( Target.Center );

		if ( (targetCenter - sourceCenter).IsNearZeroLength )
		{
			return null;
		}

		var tangent = (targetCenter - sourceCenter).Normal;

		var start = sourceCenter + tangent * Source.Radius;
		var end = targetCenter - tangent * (Target?.Radius ?? 0f);

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
		Conditional,
		Event
	}

	private (TransitionKind Kind, string? Icon, string? Title) GetLabelParts()
	{
		if ( Transition is null )
		{
			return (TransitionKind.Default, null, null);
		}

		return Transition.Condition.TryGetActionGraphImplementation( out var graph, out _ )
			? (TransitionKind.Conditional, graph.Icon ?? "filter_alt", graph.Title)
			: (TransitionKind.Default, null, null);
	}

	protected override void OnPaint()
	{
		if ( GetLocalStartEnd() is not var (start, end, tangent) )
		{
			return;
		}

		var color = Selected 
			? Color.Yellow : Hovered
			? Color.White : Color.White.Darken( 0.125f );

		Paint.SetPen( color, Selected || Hovered ? 6f : 4f );
		Paint.DrawLine( start + tangent * 12f, end - tangent * 16f );

		Paint.ClearPen();
		Paint.SetBrush( color );
		Paint.DrawArrow( end - tangent * 16f, end, 12f );

		var (kind, icon, title) = GetLabelParts();

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

			case TransitionKind.Conditional:
				Paint.DrawCircle( new Rect( -width * 0.5f - 11f, -11f, 22f, 22f ) );
				break;

			case TransitionKind.Event:
				Paint.DrawRect( new Rect( -width * 0.5f - 10f, -8f, 20f, 16f ) );
				break;
		}

		var textFlags = TextFlag.SingleLine;

		if ( tangent.x < 0f )
		{
			Paint.Rotate( 180f );

			textFlags |= TextFlag.RightBottom;
		}
		else
		{
			textFlags |= TextFlag.LeftBottom;
		}

		Paint.ClearBrush();
		Paint.SetPen( color );
		Paint.SetFont( "roboto", 12f );

		var rect = new Rect( -width * 0.5f + 16f, -20f, width - 32f, 16f );

		if ( icon is not null )
		{
			rect = rect.Shrink( 24f, 0f, 0f, 0f );

			var textRect = Paint.MeasureText( rect, title ?? "", textFlags );
			var iconRect = new Rect( textRect.Left - 24f, rect.Top - 4f, 20f, 20f );

			Paint.DrawIcon( iconRect, icon, 16f );
		}

		if ( title is not null )
		{
			Paint.DrawText( rect, title, textFlags );
		}
	}

	public void Layout()
	{
		var rect = Rect.FromPoints( Source.Center, Target?.Center ?? TargetPosition ).Grow( 64f );

		Position = rect.Position;
		Size = rect.Size;

		Update();
	}
}
