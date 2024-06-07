using System;
using System.Collections.Generic;
using Sandbox.Diagnostics;

namespace Sandbox.Events;

[Title( "State Machine" ), Category( "State Machines" )]
public sealed class StateMachineComponent : Component
{
	private Guid _currentStateGuid;

	[HostSync]
	private Guid CurrentStateGuid
	{
		get => _currentStateGuid;
		set
		{
			if ( _currentStateGuid == value ) return;

			var prev = _currentStateGuid;
			_currentStateGuid = value;

			OnCurrentStateGuidChanged( prev, value );
		}
	}

	[HostSync]
	private Guid NextStateGuid { get; set; }

	[HostSync]
	public float NextStateTime { get; set; }

	[Property]
	public StateComponent? CurrentState
	{
		get => Scene.Directory.FindComponentByGuid( CurrentStateGuid ) as StateComponent;
		set => CurrentStateGuid = value?.Id ?? Guid.Empty;
	}

	public StateComponent? NextState
	{
		get => Scene.Directory.FindComponentByGuid( NextStateGuid ) as StateComponent;
		private set => NextStateGuid = value?.Id ?? Guid.Empty;
	}

	public IEnumerable<StateComponent> States => Components.GetAll<StateComponent>( FindMode.EverythingInSelfAndDescendants );

	protected override void OnStart()
	{
		var current = CurrentState;

		foreach ( var state in States )
		{
			if ( state.Active && state != current )
			{
				state.Disable();
			}
		}

		if ( Networking.IsHost && current is not null )
		{
			Transition( current, 0f );
		}
	}

	private void OnCurrentStateGuidChanged( Guid oldValue, Guid newValue )
	{
		var oldState = Scene.Directory.FindComponentByGuid( oldValue ) as StateComponent;
		var newState = Scene.Directory.FindComponentByGuid( newValue ) as StateComponent;

		oldState?.Disable();
		newState?.Enable();
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( CurrentState is not { } current )
		{
			return;
		}

		Scene.DispatchGameEvent( new UpdateStateEventArgs( current ) );

		if ( NextState is not { } next || !(Time.Now >= NextStateTime) )
		{
			return;
		}

		if ( next.DefaultNextState is not null )
		{
			Transition( next.DefaultNextState, next.DefaultDuration );
		}
		else
		{
			ClearTransition();
		}

		CurrentState = next;
	}

	/// <summary>
	/// Queue up a transition to the given state. This will occur at the end of
	/// a fixed update on the state machine.
	/// </summary>
	public void Transition( StateComponent next, float delaySeconds = 0f )
	{
		Assert.NotNull( next );
		Assert.True( Networking.IsHost );

		NextState = next;
		NextStateTime = Time.Now + delaySeconds;
	}

	public void ClearTransition()
	{
		Assert.True( Networking.IsHost );

		NextState = null;
		NextStateTime = float.PositiveInfinity;
	}
}
