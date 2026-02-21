using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace NeoConsole
{
	public static class Tools
	{
		public static string Separator() {
			return string.Concat(Enumerable.Repeat("-", 100));
		}
		public static void ConsoleClear()
		{
			Console.Clear();
		}

		public static void ConsolePrompt(Context _CTX)
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write("ND#> ");
			Console.ResetColor();
		}
		public static void ConsoleWrite(Context _CTX, string? _data, bool _clear, ConsoleColor? _color)
		{
			if (_color == null) { _color = ConsoleColor.White; }
			Console.ForegroundColor = (ConsoleColor)_color;
			if (_clear) { Console.Clear(); }
			if (!string.IsNullOrWhiteSpace(_data)) { Console.WriteLine(_data); }
			Console.ResetColor();
			ConsolePrompt(_CTX);
		}

		public static StringBuilder Help(Context _CTX)
		{
			StringBuilder _sb = new StringBuilder();
			_sb.AppendLine(Separator());
			_sb.AppendLine("COMANDOS DISPONIBLES");
			_sb.AppendLine(Separator());
			_sb.AppendLine("* Comandos de línea:");
			_sb.AppendLine("     Help()            -> Muestra esta pantalla de ayuda");
			_sb.AppendLine("     Clear()           -> Borra la pantalla y el buffer");
			_sb.AppendLine("     Exit()            -> Cierra la aplicación");
			_sb.AppendLine(Environment.NewLine);

			_sb.AppendLine("* Funciones definidas:");
			foreach (MethodInfo x in _CTX.Methods)
			{
				string paramsStr = string.Join(", ", x.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
				_sb.AppendLine($"     {x.Name.PadRight(15)} -> ({string.Join(", ", paramsStr)})");
			}
			if (_CTX.Status != null)
			{
				_sb.AppendLine(Environment.NewLine);
				_sb.AppendLine("* Funciones dinámicas:");

				Script scriptActual = _CTX.Status.Script;
				while (scriptActual != null)
				{
					Compilation compilacion = scriptActual.GetCompilation();
					var simbolos = compilacion.GetSymbolsWithName(s => true, SymbolFilter.Member).OfType<IMethodSymbol>().Where(m => !m.IsImplicitlyDeclared && m.MethodKind == MethodKind.Ordinary);
					foreach (var s in simbolos)
					{
						_sb.AppendLine($"     {s.Name.ToString().PadRight(15)} -> (");
						for (int qp = 0; qp < s.Parameters.Length; qp++) { _sb.AppendLine($"{s.Parameters[qp]}"); }
						_sb.AppendLine(")");
					}
					scriptActual = scriptActual.Previous;
				}
			}
			_sb.AppendLine(Separator());
			_sb.AppendLine($"ND# Timestamp: {DateTime.Now.ToString()}");
			_sb.AppendLine(Separator());

			return _sb;
		}
		public static State PrepareContext(Context _CTX)
		{
			_CTX.State = new State();
			StringBuilder _sb = new StringBuilder();
			string codeToExec = _CTX.Input;
			foreach (MethodInfo Method in _CTX.Methods)
			{
				string patron = $@"\b{Method.Name}\s*\((.*?)\)";
				MatchCollection matches = Regex.Matches(codeToExec, patron);
				foreach (Match m in matches)
				{
					string _originals = m.Groups[1].Value;
					if (!string.IsNullOrWhiteSpace(_originals))
					{
						ParameterInfo[] paramInfo = Method.GetParameters();
						string[] arguments = _originals.Split(',');

						if (arguments.Length == paramInfo.Length)
						{
							for (int i = 0; i < paramInfo.Length; i++)
							{
								if (paramInfo[i].ParameterType == typeof(string) && !arguments[i].Trim().StartsWith("\"")) { 
									arguments[i] = $"({arguments[i].Trim()}).ToString()"; 
								}
							}
							codeToExec = codeToExec.Replace(m.Value, $"{Method.Name}({string.Join(", ", arguments)})");
						}
					}
				}
			}
			_sb.Append(codeToExec);

			List<string> segments = Regex.Matches(_sb.ToString(), @"[\""].+?[\""]|[^ ]+")
				.Cast<Match>()
				.Select(m => m.Value.Replace("\"", "")) // Quitamos las comillas al final
				.ToList();

			/*-------------------------------------------------------------------------------------------*/
			/*Asigna valores a la estructura de retorno*/
			/*-------------------------------------------------------------------------------------------*/
			_CTX.State.CodeVerified = _sb;
			_CTX.State.LastChar = _sb[_sb.Length - 1].ToString();
			_CTX.State.CommandName = segments[0];
			_CTX.State.Arguments = segments.Skip(1).ToArray();
			_CTX.State.Method = _CTX.GetMethodByName(_CTX.State.CommandName);
			/*-------------------------------------------------------------------------------------------*/

			return _CTX.State;
		}
	}
}
