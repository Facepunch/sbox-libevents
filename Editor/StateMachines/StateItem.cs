using System;
using System.Collections.Generic;
using Editor;

namespace Sandbox.Events.Editor;

public sealed class StateItem : GraphicsItem
{
	private readonly List<TransitionItem> _transitions = new();

	public static Color PrimaryColor { get; } = Color.Parse( "#726BF5" )!.Value;

	public StateMachineView View { get; }
	public StateComponent State { get; }

	public IEnumerable<TransitionItem> Transitions => _transitions;

	public float Radius => 64f;

	public event Action? PositionChanged;

	public StateItem( StateMachineView view, StateComponent state )
	{
		View = view;
		State = state;

		Size = new Vector2( Radius * 2f, Radius * 2f );
	}

	protected override void OnPaint()
	{
		Paint.SetPen( Color.White.WithAlpha( 0.75f ), 2f );
		Paint.SetBrushRadial( LocalRect.Center - LocalRect.Size * 0.125f, Radius * 1.5f, PrimaryColor.Lighten( 0.5f ), PrimaryColor.Darken( 0.75f ) );

		Paint.DrawCircle( Size * 0.5f, Size );

		Paint.ClearBrush();
		Paint.SetPen( Color.White );

		Paint.SetFont( null, 12f, 600 );
		Paint.DrawText( new Rect( 0f, 0f, Size.x, Size.y ), State.GameObject.Name );
	}

	public void UpdatePosition()
	{
		Position = State.Transform.LocalPosition;

		PositionChanged?.Invoke();
	}

	public void UpdateTransitions()
	{
		foreach ( var transition in _transitions )
		{
			transition.Destroy();
		}

		_transitions.Clear();

		foreach ( var transitionSource in State.GameObject.Components.GetAll<ITransitionSource>( FindMode.EverythingInSelf ) )
		{
			foreach ( var transition in transitionSource.Transitions )
			{
				var item = new TransitionItem( this, View.GetStateItem( transition.TargetState ) );
				View.Add( item );

				_transitions.Add( item );
			}
		}
	}
}
