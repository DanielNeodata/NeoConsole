using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NeoConsole
{
	public static class Tools
	{
		public static void ConsolePrompt(Context _CTX)
		{
			Console.ForegroundColor = _CTX.indent > 0 ? ConsoleColor.Yellow : ConsoleColor.Cyan;
			Console.Write(_CTX.indent > 0 ? "... " : "C#> ");
			Console.ResetColor();
		}
		public static void ConsoleWrite(string? _data, bool _clear, ConsoleColor? _color)
		{
			if (_color == null) { _color = ConsoleColor.White; }
			Console.ForegroundColor = (ConsoleColor)_color;
			if (_clear) { Console.Clear(); }
			if (!string.IsNullOrWhiteSpace(_data)) { Console.WriteLine(_data); }
			Console.ResetColor();
		}

		public static StringBuilder Help(Context _CTX)
		{
			StringBuilder _sb = new StringBuilder();
			_sb.AppendLine("=== C# AVANZADO (Multilínea) ===");
			_sb.AppendLine("Si escribes ';' o '}' al final de la linea, ejecuta la sentencia y la mantiene en el Buffer");
			_sb.AppendLine("Si escribes la sentencia sin ';' al final ejecuta el método que esté disponible o da error.");
			_sb.AppendLine("--- COMANDOS DISPONIBLES ---");
			foreach (MethodInfo x in _CTX.Methods)
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
		public static StringBuilder ListDynMethods(Context _CTX)
		{
			StringBuilder _sb = Help(_CTX);
			if (_CTX.Status != null)
			{
				// Recorremos todos los scripts en la cadena (del más nuevo al más viejo)
				Script scriptActual = _CTX.Status.Script;
				HashSet<string> methodViewed = new HashSet<string>(); // Para evitar duplicados si se redefine algo

				while (scriptActual != null)
				{
					Compilation compilacion = scriptActual.GetCompilation();
					// Buscamos los símbolos en esta sumisión específica
					var simbolos = compilacion.GetSymbolsWithName(s => true, SymbolFilter.Member)
											  .OfType<IMethodSymbol>()
											  .Where(m => !m.IsImplicitlyDeclared && m.MethodKind == MethodKind.Ordinary);
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
		public static State FixTypes(Context _CTX)
		{
			State _state = new State();
			StringBuilder _sb = new StringBuilder();
			string codeToExec = _CTX.bufferCode.ToString();
			foreach (MethodInfo Method in _CTX.Methods)
			{
				// Buscamos si el nombre del método aparece en el código del usuario
				// Ejemplo: Saludar(123)
				string patron = $@"\b{Method.Name}\s*\((.*?)\)";
				MatchCollection matches = Regex.Matches(codeToExec, patron);

				foreach (Match m in matches)
				{
					string argumentosOriginales = m.Groups[1].Value;
					if (!string.IsNullOrWhiteSpace(argumentosOriginales))
					{
						ParameterInfo[] paramInfo = Method.GetParameters();
						string[] arguments = argumentosOriginales.Split(',');

						if (arguments.Length == paramInfo.Length)
						{
							for (int i = 0; i < paramInfo.Length; i++)
							{
								// Si el método espera string pero el usuario NO puso comillas
								if (paramInfo[i].ParameterType == typeof(string) && !arguments[i].Trim().StartsWith("\"")) { arguments[i] = $"({arguments[i].Trim()}).ToString()"; }
							}
							// Reconstruimos la llamada al método reparada
							codeToExec = codeToExec.Replace(m.Value, $"{Method.Name}({string.Join(", ", arguments)})");
						}
					}
				}
			}
			codeToExec = codeToExec.Replace("(", " ").Replace(")", " ").Replace("\r", "").Replace("\n", "");
			_sb.Append(codeToExec);

			/*Reset por seguridad del buffer*/
			_CTX.bufferCode.Clear();

			/*Reset por seguridad del indent*/
			_CTX.indent = 0;

			/*Regex mágica para separar por espacios pero respetar lo que está entre comillas*/
			/*REVISAR!!!!*/
			List<string> segments = Regex.Matches(_sb.ToString(), @"[\""].+?[\""]|[^ ]+")
				.Cast<Match>()
				.Select(m => m.Value.Replace("\"", "")) // Quitamos las comillas al final
				.ToList();

			/*-------------------------------------------------------------------------------------------*/
			/*Asigna valores a la estructura de retorno*/
			/*-------------------------------------------------------------------------------------------*/
			_state.CodeVerified = _sb;
			_state.LastChar = _sb[_sb.Length - 1].ToString();
			_state.CommandName = segments[0];
			_state.Arguments = segments.Skip(1).ToArray();
			_state.Method = _CTX.GetMethodByName(_state.CommandName);
			_state.Exists = false;
			_state.Exists = false;
			/*-------------------------------------------------------------------------------------------*/

			return _state;
		}
		public static bool Exec(Context _CTX)
		{
			bool _continue = true;
			StringBuilder _sb = new StringBuilder();
			if (string.IsNullOrWhiteSpace(_CTX.Input) && _CTX.bufferCode.Length == 0) { _CTX.Input = "null"; }
			try
			{
				switch (_CTX.Input)
				{
					case "null":
						_sb = ListDynMethods(_CTX);
						break;
					case "salir":
					case "exit":
					case "quit":
						_continue = false;
						break;
					case "cls":
						throw new Exception("cls");
					case "clear":
						_CTX.bufferCode.Clear();
						_CTX.indent = 0;
						_CTX.Status = null;
						throw new Exception("clear");
					default:
						break;
				}
			}
			catch (Exception ex)
			{
				_sb = Help(_CTX);
			}
			Tools.ConsoleWrite(_sb.ToString(), true, null);
			return _continue;
		}
	}
}
