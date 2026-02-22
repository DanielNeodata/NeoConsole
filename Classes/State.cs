using System.Reflection;
using System.Text;

namespace NeoConsole.Classes
{
	public class State
	{
		/*Propiedades*/
		public MethodInfo Method { get; set; }
		public string LastChar { get; set; }
		public StringBuilder CodeVerified { get; set; }
		public string CommandName { get; set; }
		public object[] Arguments { get; set; }
		public object? ReturnValue { get; set; }
	}
}
