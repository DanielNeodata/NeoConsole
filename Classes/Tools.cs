using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NeoConsole.Classes
{
	public static class Tools
	{
		public static void ConsoleClear()
		{
			Console.Clear();
		}
		public static void ConsolePrompt(Context _CTX)
		{
			Console.ForegroundColor = ConsoleColor.DarkCyan;
			Console.WriteLine(Environment.NewLine);
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
		}
		public static void ConsoleReturnValue(Context _CTX)
		{
			ConsoleWrite(_CTX, Separator(), true, null);
			ConsoleWrite(_CTX, $"RESPUESTA", false, null);
			ConsoleWrite(_CTX, Separator(), false, null);
			ConsoleWrite(_CTX, $"{_CTX.State.ReturnValue}", false, ConsoleColor.Cyan);
			ConsoleWrite(_CTX, Separator(), false, null);
			ConsoleWrite(_CTX, $"ND# Timestamp: {DateTime.Now.ToString()}", false, null);
			ConsoleWrite(_CTX, Separator(), false, null);
		}
		public static void ConsoleError(Context _CTX, Exception ex)
		{
			ConsoleWrite(_CTX, $"[Error]: {ex.Message}", true, ConsoleColor.Red);
		}

		public static string Separator()
		{
			return string.Concat(Enumerable.Repeat("-", 100));
		}
		public static string ListMethods(MethodInfo[] _methods, string _title)
		{
			StringBuilder _sb = new StringBuilder();
			_sb.AppendLine(_title);
			foreach (MethodInfo x in _methods)
			{
				string paramsStr = string.Join(", ", x.GetParameters().Select(p => $"{p.ParameterType.Name.ToLower()} {p.Name}"));
				CustomDescriptionAttribute attribute = (CustomDescriptionAttribute)Attribute.GetCustomAttribute(x, typeof(CustomDescriptionAttribute));
				string paramCustom = "";
				if (attribute != null) { paramCustom = $"-> ({attribute.Description})"; }
				_sb.AppendLine($"   {x.Name}({string.Join(", ", paramsStr)}) {paramCustom}");
			}
			return _sb.ToString();
		}
		public static StringBuilder Help(Context _CTX)
		{
			StringBuilder _sb = new StringBuilder();
			_sb.AppendLine(Separator());
			_sb.AppendLine("AYUDA");
			_sb.AppendLine(Separator());

			_sb.AppendLine("* Prefijos de acción:");
			foreach (KeyValuePair<string, Info> entry in _CTX.Prefixs) { _sb.AppendLine($"   {entry.Value.Key} -> ({entry.Value.Description})"); }
			_sb.AppendLine("");

			_sb.AppendLine("* Contextos disponibles:");
			foreach (KeyValuePair<string, Info> entry in _CTX.Contexts) { _sb.AppendLine($"   {entry.Value.Key} -> ({entry.Value.Description})"); }
			_sb.AppendLine("");

			_sb.AppendLine(ListMethods(_CTX.MethodsAbstract, "* Funciones abstractas:"));
			_sb.AppendLine(ListMethods(_CTX.Methods, "* Funciones definidas:"));

			if (_CTX.Status != null)
			{
				_sb.AppendLine("* Funciones dinámicas:");
				Script scriptActual = _CTX.Status.Script;
				while (scriptActual != null)
				{
					Compilation compilation = scriptActual.GetCompilation();
					IEnumerable<ISymbol> symbols = compilation.GetSymbolsWithName(s => true, SymbolFilter.Member).OfType<IMethodSymbol>().Where(m => !m.IsImplicitlyDeclared && m.MethodKind == MethodKind.Ordinary);
					foreach (ISymbol s in symbols)
					{
						string _params = "";
						foreach (var p in ((IMethodSymbol)s.OriginalDefinition).Parameters) { _params += ($"{p.ToString()}, "); }
						char[] _t = { ',', ' ' };
						_sb.AppendLine($"   {s.Name.ToString()}({_params.TrimEnd(_t)})");
					}
					scriptActual = scriptActual.Previous;
				}
			}
			_sb.AppendLine(Separator());
			_sb.AppendLine($"ND# Timestamp: {DateTime.Now.ToString()} Contexto activo: {_CTX.Key}");
			_sb.AppendLine(Separator());

			return _sb;
		}
		public static object ConvertStringToParameterType(string value, ParameterInfo parameterInfo)
		{
			Type targetType = parameterInfo.ParameterType;
			if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				if (value == null) { return null; }
				targetType = Nullable.GetUnderlyingType(targetType);
			}
			try
			{
				return Convert.ChangeType(value, targetType);
			}
			catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
			{
				return null;
			}
		}
		public static State PrepareContext(Context _CTX)
		{
			_CTX.State = new State();
			StringBuilder _sb = new StringBuilder();
			string codeToExec = _CTX.Input;
			_sb.Append(codeToExec);

			/*-------------------------------------------------------------------------------------------*/
			/*Asigna valores a la estructura de retorno*/
			/*-------------------------------------------------------------------------------------------*/
			string[] segments = codeToExec.Split('(');
			string[] arguments = Array.Empty<string>();

			if (segments.Length > 1) { arguments = segments[1].Replace(")", "").Trim().Split(','); }

			_CTX.State.CodeVerified = _sb;
			_CTX.State.LastChar = _sb[_sb.Length - 1].ToString();
			_CTX.State.CommandName = segments[0];
			_CTX.State.Method = _CTX.GetMethodByName(segments[0]);
			_CTX.State.Arguments = Array.Empty<object>();
			if (_CTX.State.Method != null && arguments.Length != 0)
			{
				ParameterInfo[] paramInfo = _CTX.State.Method.GetParameters();
				for (int i = 0; i < paramInfo.Length; i++)
				{
					_CTX.State.Arguments = _CTX.State.Arguments.Append(ConvertStringToParameterType(arguments[i], paramInfo[i])).ToArray();
				}
			}
			/*-------------------------------------------------------------------------------------------*/

			return _CTX.State;
		}
	}
}
