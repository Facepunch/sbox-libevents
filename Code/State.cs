using System.Collections.Generic;

namespace Sandbox.Events;

[Title( "State" ), Category( "State Machines" )]
public sealed class StateComponent : Component
{
	private StateMachineComponent? _stateMachine;

	public StateMachineComponent StateMachine =>
		_stateMachine ??= Components.GetInAncestorsOrSelf<StateMachineComponent>();

	public StateComponent? Parent => Components.GetInAncestors<StateComponent>( true );

	/// <summary>
	/// Transition to this state by default.
	/// </summary>
	[Property]
	public StateComponent? DefaultNextState { get; set; }

	[Property, HideIf( nameof( DefaultNextState ), null )]
	public float DefaultDuration { get; set; }

	/// <summary>
	/// Queue up a transition to the given state. This will occur at the end of
	/// a fixed update on the state machine.
	/// </summary>
	public void Transition( StateComponent next, float delaySeconds = 0f )
	{
		StateMachine.Transition( next, delaySeconds );
	}

	/// <summary>
	/// Queue up a transition to the default next state.
	/// </summary>
	public void Transition()
	{
		StateMachine.Transition( DefaultNextState! );
	}

	/// <summary>
	/// Gets all ancestor states up to and including this one, starting
	/// with the outermost parent and ending with this state.
	/// </summary>
	public IReadOnlyList<StateComponent> GetAncestorsIncludingSelf()
	{
		var list = new List<StateComponent> { this };

		var parent = Parent;

		while ( parent != null )
		{
			list.Add( parent );
			parent = parent.Parent;
		}

		list.Reverse();

		return list;
	}
}

/// <summary>
/// Event dispatched on the host when a <see cref="StateMachineComponent"/> changes state.
/// Only invoked on components on the same object as the new state.
/// </summary>
public record EnterStateEventArgs( StateComponent State );

/// <summary>
/// Event dispatched on the host when a <see cref="StateMachineComponent"/> changes state.
/// Only invoked on components on the same object as the old state.
/// </summary>
public record LeaveStateEventArgs( StateComponent State );

/// <summary>
/// Event dispatched on the host every fixed update while a <see cref="StateComponent"/> is active.
/// Only invoked on components on the same object as the state.
/// </summary>
public record UpdateStateEventArgs( StateComponent State );
