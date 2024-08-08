using System;
using System.Linq;
using Editor;
using Editor.NodeEditor;
using Facepunch.ActionGraphs;

namespace Sandbox.Events.Editor;

public sealed partial class TransitionItem : GraphicsItem, IContextMenuSource, IDeletable, IComparable<TransitionItem>
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

		return Math.Abs( s ) <= 24f;
	}

	private void OnStatePositionChanged()
	{
		Layout();
	}

	private (Vector2 Start, Vector2 End, Vector2 Tangent)? GetLocalStartEnd()
	{
		var (index, count) = Source.View.GetTransitionPosition( this );

		var sourceCenter = FromScene( Source.Center );
		var targetCenter = Target is null ? FromScene( TargetPosition ) : FromScene( Target.Center );

		if ( (targetCenter - sourceCenter).IsNearZeroLength )
		{
			return null;
		}

		var tangent = (targetCenter - sourceCenter).Normal;
		var normal = tangent.Perpendicular;

		if ( Target is null || Target.State.Id.CompareTo( Source.State.Id ) < 0 )
		{
			normal = -normal;
		}

		var maxWidth = Source.Radius * 2f;
		var usedWidth = count * 24f;

		var itemWidth = Math.Min( usedWidth, maxWidth ) / count;
		var offset = (index - count * 0.5f + 0.5f) * itemWidth;
		var curve = MathF.Sqrt( Source.Radius * Source.Radius - offset * offset );

		var start = sourceCenter + tangent * curve;
		var end = targetCenter - tangent * (Target is null ? 0f : curve);

		return (start + offset * normal, end + offset * normal, tangent);
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

	private TransitionKind GetTransitionKind()
	{
		if ( Transition?.Condition.TryGetActionGraphImplementation( out var graph, out _ ) is true )
		{
			return TransitionKind.Conditional;
		}

		return TransitionKind.Default;
	}

	private (string? Icon, string? Title) GetLabelParts( Delegate? deleg, string defaultIcon )
	{
		return deleg.TryGetActionGraphImplementation( out var graph, out _ )
			? (string.IsNullOrEmpty( graph.Icon ) ? defaultIcon : graph.Icon, graph.Title)
			: (null, null);
	}

	protected override void OnPaint()
	{
		if ( GetLocalStartEnd() is not var (start, end, tangent) )
		{
			return;
		}

		var normal = tangent.Perpendicular;

		var selected = Selected || Source.Selected || Transition is null;
		var hovered = Hovered || Source.Hovered;

		var thickness = selected || hovered ? 6f : 4f;
		var offset = thickness * 0.5f * normal;

		var color = selected
			? Color.Yellow : hovered
			? Color.White : Color.White.Darken( 0.125f );

		Paint.ClearPen();
		Paint.SetBrushLinear( start, end, color.Darken( 0.667f ), color );
		Paint.DrawPolygon( start - offset, end - tangent * 14f - offset, end - tangent * 14f + offset, start + offset );

		Paint.SetBrush( color );
		Paint.DrawArrow( end - tangent * 16f, end, 12f );

		var mid = (start + end) * 0.5f;
		var width = (end - start).Length;

		Paint.Translate( mid );
		Paint.Rotate( MathF.Atan2( tangent.y, tangent.x ) * 180f / MathF.PI );

		Paint.ClearBrush();
		Paint.SetPen( color );
		Paint.SetFont( "roboto", 10f );

		var conditionRect = new Rect( -width * 0.5f + 16f, -20f, width - 32f, 16f );
		var actionRect = new Rect( -width * 0.5f + 16f, 4f, width - 32f, 16f );

		if ( tangent.x < 0f )
		{
			Paint.Rotate( 180f );

			DrawLabel( Transition?.Condition, "question_mark", conditionRect, TextFlag.SingleLine | TextFlag.RightBottom );
			DrawLabel( Transition?.Action, "directions_run", actionRect, TextFlag.SingleLine | TextFlag.LeftTop );
		}
		else
		{
			DrawLabel( Transition?.Condition, "question_mark", conditionRect, TextFlag.SingleLine | TextFlag.LeftBottom );
			DrawLabel( Transition?.Action, "directions_run", actionRect, TextFlag.SingleLine | TextFlag.RightTop );
		}
	}

	private void DrawLabel( Delegate? deleg, string defaultIcon, Rect rect, TextFlag flags )
	{
		var (icon, title) = GetLabelParts( deleg, defaultIcon );

		if ( icon is not null )
		{
			rect = rect.Shrink( 20f, 0f, 0f, 0f );

			var textRect = Paint.MeasureText( rect, title ?? "", flags );
			var iconRect = new Rect( textRect.Left - 18f, rect.Top, 16f, 16f );

			Paint.DrawIcon( iconRect, icon, 12f );
		}

		if ( title is not null )
		{
			Paint.DrawText( rect, title, flags );
		}
	}

	public void Layout()
	{
		var rect = Rect.FromPoints( Source.Center, Target?.Center ?? TargetPosition ).Grow( 64f );

		Position = rect.Position;
		Size = rect.Size;

		Update();
	}

	public void Delete()
	{
		Transition!.Destroy();
		Destroy();
	}

	private T CreateGraph<T>( string title )
		where T : Delegate
	{
		var graph = ActionGraph.Create<T>( EditorNodeLibrary );
		var inner = (ActionGraph)graph;

		inner.Title = title;
		inner.SetParameters(
			inner.Inputs.Values.Concat( InputDefinition.Target( typeof( GameObject ), Transition!.GameObject ) ),
			inner.Outputs.Values.ToArray() );

		return graph;
	}

	private void EditGraph<T>( T action )
		where T : Delegate
	{
		if ( action.TryGetActionGraphImplementation( out var graph, out _ ) )
		{
			EditorEvent.Run( "actiongraph.inspect", graph );
		}
	}

	public void OnContextMenu( ContextMenuEvent e )
	{
		if ( Transition is null ) return;

		e.Accepted = true;
		Selected = true;

		var menu = new global::Editor.Menu();

		if ( Transition.Condition is not null )
		{
			menu.AddOption( "Edit Condition", "edit", action: () => EditGraph( Transition.Condition ) );
			menu.AddOption( "Clear Condition", "clear", action: () =>
			{
				Transition.Condition = null;
				Update();
			} );
		}
		else
		{
			menu.AddOption( "Add Condition", "question_mark", action: () =>
			{
				Transition.Condition = CreateGraph<Func<bool>>( "Condition" );
				EditGraph( Transition.Condition );
				Update();
			} );

			menu.AddMenu( "Add Delay", "timer" ).AddLineEdit( "Seconds", value: "1", autoFocus: true, onSubmit:
				delayStr =>
				{
					if ( !float.TryParse( delayStr, out var seconds ) )
					{
						return;
					}

					Transition.Condition = CreateDelayGraph( seconds );
					Update();
				} );
		}

		menu.AddSeparator();

		if ( Transition.Action is not null )
		{
			menu.AddOption( "Edit Action", "edit", action: () => EditGraph( Transition.Action ) );
			menu.AddOption( "Clear Action", "clear", action: () =>
			{
				Transition.Action = null;
				Update();
			} );
		}
		else
		{
			menu.AddOption( "Add Action", "directions_run", action: () =>
			{
				Transition.Action = CreateGraph<Action>( "Action" );
				EditGraph( Transition.Action );
				Update();
			} );
		}

		menu.AddSeparator();

		menu.AddOption( "Delete Transition", "delete", action: Delete );

		menu.OpenAtCursor( true );
	}

	public int CompareTo( TransitionItem? other )
	{
		return Source.State.Id.CompareTo( other?.Source.State.Id ?? Guid.Empty );
	}
}
