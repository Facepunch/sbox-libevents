using System;

namespace Sandbox.Events;

public interface ITransition
{
	StateComponent? Target { get; }

	void Remove();
}

public abstract class BaseTransition : Component, ITransition
{
	/// <summary>
	/// The state this transition is originating from.
	/// </summary>
	[RequireComponent] public StateComponent Source { get; private set; } = null!;

	/// <summary>
	/// The destination of this transition.
	/// </summary>
	[Property] public StateComponent Target { get; set; } = null!;

	void ITransition.Remove()
	{
		Destroy();
	}
}

public class ImmediateTransition : BaseTransition
{
	/// <summary>
	/// Optional condition to evaluate. This transition will be taken if the condition
	/// is null, or evaluates to true.
	/// </summary>
	[Property, KeyProperty] public Func<bool>? Condition { get; set; }

	/// <summary>
	/// If multiple immediate transitions are valid, the one with the highest priority is chosen.
	/// </summary>
	[Property] public int Priority { get; set; }

	/// <summary>
	/// If multiple immediate transitions with the same <see cref="Priority"/> are valid,
	/// a random one is chosen, weighted by this value.
	/// </summary>
	[Property] public float Weight { get; set; } = 1f;
}

/// <summary>
/// A transition that triggers after receiving an event of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Game event type to transition after.</typeparam>
public class GameEventTransition<T> : BaseTransition
	where T : IGameEvent
{
	/// <summary>
	/// Optional condition to test on each event.
	/// </summary>
	[Property, KeyProperty]
	public Predicate<T>? Condition { get; set; }
}
