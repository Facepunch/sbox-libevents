using System.Collections.Generic;

namespace Sandbox.Events;

public interface ITransition
{
	StateComponent TargetState { get; }
}

public interface ITransitionSource
{
	IEnumerable<ITransition> Transitions { get; }
}
