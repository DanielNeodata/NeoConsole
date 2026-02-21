using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NeoConsole
{
	public class Context
	{
		/*Propiedades*/
		public string Key { get; set; }
		public string? Input { get; set; }
		public State State { get; set; }
		public ScriptOptions Options { get; set; }
		public ScriptState<object> Status { get; set; }
		public dynamic Commands { get; set; }
		public MethodInfo[] Methods { get; set; }

		/*Constructor*/
		public Context(string _key, object _class)
		{
			Key = _key;
			Type type = Type.GetType(_class.ToString());
			Commands = Activator.CreateInstance(type);
			Methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			Options = ScriptOptions.Default.AddReferences(type.Assembly).AddImports("System", "System.Text");
		}

		/*Métodos*/
		public MethodInfo GetMethodByName(string commandName)
		{
			return Methods.FirstOrDefault(m => m.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
		}
	}
}
