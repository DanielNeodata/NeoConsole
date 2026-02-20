using Microsoft.CodeAnalysis.Scripting;
using System;
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
		public ScriptOptions Options { get; set; }
		public ScriptState<object> Status { get; set; }
		public object Commands { get; set; }
		public MethodInfo[] Methods { get; set; }
		public StringBuilder bufferCode { get; set; }
		public int indent { get; set; }

		/*Constructor*/
		public Context(string _key, string _class)
		{
			Type type = Type.GetType(_class);
			Key = _key;
			Commands = Activator.CreateInstance(type);
			Methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			Options = ScriptOptions.Default.AddReferences(type.Assembly).AddImports("System", "System.Text");
			bufferCode = new StringBuilder();
		}

		/*Métodos*/
		public MethodInfo GetMethodByName(string commandName)
		{
			return Methods.FirstOrDefault(m => m.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
		}
		public void ConsolePrompt()
		{
			Tools.ConsolePrompt(this);
		}
	}
}
