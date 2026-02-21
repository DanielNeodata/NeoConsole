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
		public Type Type { get; set; }
		public Abstract Abstract { get; set; }
		public Type TypeAbstract { get; set; }
		public MethodInfo[] MethodsAbstract { get; set; }

		/*Constructor*/
		public Context(string _key, object _class)
		{
			Key = _key;
			Type = Type.GetType(_class.ToString());
			Commands = Activator.CreateInstance(Type);
			Methods = Type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			Options = ScriptOptions.Default.AddReferences(Type.Assembly).AddImports("System", "System.Text");
			Abstract = new Abstract();
			TypeAbstract = Type.GetType(Abstract.ToString());
			MethodsAbstract = TypeAbstract.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		}

		/*Métodos*/
		public MethodInfo GetMethodByName(string commandName)
		{
			MethodInfo _m = Methods.FirstOrDefault(m => m.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
			if (_m == null) { _m = MethodsAbstract.FirstOrDefault(m => m.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase)); }
			return _m;
		}
	}
}
