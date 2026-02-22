using Microsoft.CodeAnalysis.Scripting;
using NeoConsole.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NeoConsole.Classes
{
	public class Context
	{
		/*Propiedades*/
		public string Key { get; set; }
		public string Description { get; set; }
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
		public Dictionary<string, Info> Prefixs { get; set; }
		public Dictionary<string, Info> Contexts { get; set; }

		/*Constructor*/
		public Context(string _key, string _class, string _description, Dictionary<string, Info> _prefixs, Dictionary<string, Info> _contexts)
		{
			Key = _key;
			Type = Type.GetType(_class.ToString());
			Commands = Activator.CreateInstance(Type);
			Methods = Type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			Options = ScriptOptions.Default.AddReferences(Type.Assembly).AddImports("System", "System.Text");
			Abstract = new Abstract();
			TypeAbstract = Type.GetType(Abstract.ToString());
			MethodsAbstract = TypeAbstract.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			Description = _description;
			Prefixs = _prefixs;
			Contexts = _contexts;
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
