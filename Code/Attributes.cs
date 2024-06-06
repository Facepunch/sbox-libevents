
using System;

namespace Sandbox.Events;

[AttributeUsage( AttributeTargets.Method )]
public sealed class EarlyAttribute : Attribute
{

}

[AttributeUsage( AttributeTargets.Method )]
public sealed class LateAttribute : Attribute
{

}

public interface IBeforeAttribute
{
	Type Type { get; }
}

public interface IAfterAttribute
{
	Type Type { get; }
}

[AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
public sealed class BeforeAttribute<T> : Attribute, IBeforeAttribute
{
	Type IBeforeAttribute.Type => typeof(T);
}

[AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
public sealed class AfterAttribute<T> : Attribute, IAfterAttribute
{
	Type IAfterAttribute.Type => typeof( T );
}
