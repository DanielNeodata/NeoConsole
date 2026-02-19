using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NeoConsole
{
	public class State
	{
		public string Key = "";
		public ScriptOptions Options = null;
		public ScriptState<object> Status = null;
		public Funciones Commands = null;
		public MethodInfo[] Methods = null;
		public StringBuilder bufferCode = new StringBuilder();
		public int indent = 0;

		public State(string _key, ScriptOptions _options)
		{
			Key = _key;
			Commands = new Funciones();
			Methods = typeof(Funciones).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			Options = _options;
		}
		public StringBuilder Help()
		{
			StringBuilder _sb = new StringBuilder();
			_sb.AppendLine("=== C# AVANZADO (Multilínea) ===");
			_sb.AppendLine("Si escribes ';' o '}' al final de la linea, ejecuta la sentencia y la mantiene en el Buffer");
			_sb.AppendLine("Si escribes la sentencia sin ';' al final ejecuta el método que esté disponible o da error.");

			_sb.AppendLine("--- COMANDOS DISPONIBLES ---");
			foreach (MethodInfo x in Methods)
			{
				string paramsStr = string.Join(", ", x.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
				_sb.AppendLine($"  {x.Name.PadRight(15)} -> ({string.Join(", ", paramsStr)})");
			}
			_sb.AppendLine("  Cls             -> Borra la pantalla");
			_sb.AppendLine("  Clear           -> Borra la pantalla y el buffer");
			_sb.AppendLine("  Salir           -> Cierra la aplicación");
			_sb.AppendLine("----------------------------");

			return _sb;
		}
		public StringBuilder ListDynMethods()
		{
			StringBuilder _sb = Help();
			if (Status != null)
			{
				// Recorremos todos los scripts en la cadena (del más nuevo al más viejo)
				var scriptActual = Status.Script;
				var methodViewed = new HashSet<string>(); // Para evitar duplicados si se redefine algo
				while (scriptActual != null)
				{
					Compilation compilacion = scriptActual.GetCompilation();
					// Buscamos los símbolos en esta sumisión específica
					var simbolos = compilacion.GetSymbolsWithName(s => true, SymbolFilter.Member)
											  .OfType<IMethodSymbol>()
											  .Where(m => !m.IsImplicitlyDeclared &&
														   m.MethodKind == MethodKind.Ordinary);
					foreach (var s in simbolos)
					{
						string firma = s.ToDisplayString();
						if (methodViewed.Add(firma)) // Si es nuevo en la lista, lo imprimimos
						{
							_sb.AppendLine($"  {s.Name.ToString().PadRight(15)} -> (");
							for (int qp = 0; qp < s.Parameters.Length; qp++) { _sb.AppendLine($"{s.Parameters[qp]}"); }
							_sb.AppendLine(")");
						}
					}

					// Subimos al script anterior en la cadena de ContinueWith
					scriptActual = scriptActual.Previous;
				}
			}
			return _sb;
		}
		public StringBuilder Exec(string? input, out bool _continue) {
			_continue = true;
			StringBuilder _sb = new StringBuilder();
			if (string.IsNullOrWhiteSpace(input) && bufferCode.Length == 0) { input = "null"; }
			try
			{
				switch (input)
				{
					case "null":
						_sb = ListDynMethods();
						break;
					case "salir":
					case "exit":
					case "quit":
						_continue = false;
						break;
					case "cls":
						throw new Exception("cls");
					case "clear":
						bufferCode.Clear();
						indent = 0;
						Status = null;
						throw new Exception("clear"); 
					default:
						break;
				}
			}
			catch (Exception ex) {
				_sb = Help();
			}
			return _sb;
		}
	}
}
