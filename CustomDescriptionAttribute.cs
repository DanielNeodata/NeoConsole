using System;

[AttributeUsage(AttributeTargets.Method)]
public class CustomDescriptionAttribute : Attribute
{
	public string Description { get; }

	// Constructor to accept a positional parameter (the description)
	public CustomDescriptionAttribute(string description)
	{
		Description = description;
	}
}