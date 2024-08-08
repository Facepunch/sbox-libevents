using System;

namespace Sandbox.Events;

[Title( "Transition" ), Icon( "forward" ), Category( "State Machines" )]
public sealed class TransitionComponent : Component, IComparable<TransitionComponent>
{
	/// <summary>
	/// The state this transition is originating from.
	/// </summary>
	[RequireComponent] public StateComponent Source { get; private set; } = null!;

	/// <summary>
	/// The destination of this transition.
	/// </summary>
	[Property] public StateComponent Target { get; set; } = null!;

	/// <summary>
	/// Optional condition to evaluate.
	/// </summary>
	[Property, KeyProperty] public Func<bool>? Condition { get; set; }

	/// <summary>
	/// Action performed when this transition is taken.
	/// </summary>
	[Property, KeyProperty] public Action? Action { get; set; }

	public int CompareTo( TransitionComponent? other )
	{
		if ( other is null ) return 1;
		return (Condition is null).CompareTo( other.Condition is null );
	}
}
