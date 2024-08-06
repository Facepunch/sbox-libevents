using System;
using System.Collections.Generic;
using Editor;

namespace Sandbox.Events.Editor;

public sealed class StateItem : GraphicsItem
{
	private readonly List<TransitionItem> _transitions = new();

	public static Color PrimaryColor { get; } = Color.Parse( "#5C79DB" )!.Value;
	public static Color SelectedColor { get; } = Color.Parse( "#BCA5DB" )!.Value;

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

		Movable = true;
		Selectable = true;
		HoverEvents = true;
	}

	public override bool Contains( Vector2 localPos )
	{
		return (LocalRect.Center - localPos).LengthSquared < Radius * Radius;
	}

	protected override void OnPaint()
	{
		var borderColor = Selected || Hovered
			? Color.White
			: Color.White.Darken( 0.125f );

		var fillColor = Selected
			? SelectedColor
			: Hovered ? Color.Lerp( PrimaryColor, SelectedColor, 0.5f )
			: PrimaryColor;

		Paint.SetBrushRadial( LocalRect.Center - LocalRect.Size * 0.125f, Radius * 1.5f, fillColor.Lighten( 0.5f ), fillColor.Darken( 0.75f ) );
		Paint.DrawCircle( Size * 0.5f, Size );

		Paint.SetPen( borderColor, Selected || Hovered ? 3f : 2f );
		Paint.SetBrushRadial( LocalRect.Center, Radius, 0.75f, Color.Black.WithAlpha( 0f ), 1f, Color.Black.WithAlpha( 0.25f ) );
		Paint.DrawCircle( Size * 0.5f, Size );

		Paint.ClearBrush();
		Paint.SetFont( "roboto", 12f, 600 );
		Paint.SetPen( Color.Black.WithAlpha( 0.5f ) );
		Paint.DrawText( new Rect( 2f, 2f, Size.x, Size.y ), State.GameObject.Name );

		Paint.SetPen( Color.White );
		Paint.DrawText( new Rect( 0f, 0f, Size.x, Size.y ), State.GameObject.Name );
	}

	protected override void OnMoved()
	{
		State.Transform.LocalPosition = Position.SnapToGrid( 32f );

		UpdatePosition();
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
