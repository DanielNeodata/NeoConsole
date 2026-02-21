using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeoConsole
{

	internal static class Program
	{
		internal static Dictionary<string, Context> _ALL = new Dictionary<string, Context>();
		internal static string[] _prefixs = Array.Empty<string>();
		internal static Context _CTX = null;

		static async Task Main(string[] args)
		{
			/*Carga de los contextos disponibles*/
			_ALL.Add("Benchmark", new Context("Benchmark", "NeoConsole.Benchmark", "Pruebas de performance"));
			_ALL.Add("Test", new Context("Test", "NeoConsole.Test", "Funciones de testeo"));

			/*Definición de prefijos de ejecución*/
			_prefixs = _prefixs.Append("[ctx] -> Cambiar el contexto.  [ctx]Contexto ").ToArray();
			_prefixs = _prefixs.Append("[run] -> Crear funciones en el contexto activo. [run]int fncname(string a, int b)").ToArray();

			/*Contexto por default*/
			_CTX = _ALL["Test"];
			_CTX.Contexts = _ALL.Keys.ToArray();
			_CTX.Prefixs = _prefixs;

			/*Muestra ayuda por default*/
			Tools.ConsoleWrite(_CTX, Tools.Help(_CTX).ToString(), true, null);

			/*Muestra prompt por default*/
			Tools.ConsolePrompt(_CTX);

			while (true)
			{
				string _preContext = "[ctx]";
				string _input = Console.ReadLine();
				if (_input.ToLower().StartsWith(_preContext))
				{
					_input = _input.Substring(_preContext.Length);
					if (_ALL.ContainsKey(_input))
					{
						/*Cambio de contexto según envío del usuario*/
						_CTX = _ALL[_input];
						_CTX.Contexts = _ALL.Keys.ToArray();
						_CTX.Prefixs = _prefixs;

						Tools.ConsoleWrite(_CTX, Tools.Help(_CTX).ToString(), true, null);
						Tools.ConsoleWrite(_CTX, $"Se activó el contexto: {_input}", false, ConsoleColor.Green);
						Tools.ConsolePrompt(_CTX);
					}
					else
					{
						Tools.ConsoleWrite(_CTX, $"No se existe el contexto {_input}", true, ConsoleColor.Red);
						Tools.ConsolePrompt(_CTX);
					}
				}
				else
				{
					_CTX.Input = _input;
					if (!string.IsNullOrWhiteSpace(_CTX.Input))
					{
						string _preCommand = "[run]";
						if (_CTX.Input.ToLower().StartsWith(_preCommand))
						{
							/*Quita el prefijo*/
							_CTX.Input = _CTX.Input.Substring(_preCommand.Length);

							/*Ejecucion de comando no definido en Abstract, _CTX, ni en los scripts del State del _CTX */
							await Interpreter.RunAsync(_CTX);
						}
						else
						{
							/*Llamada asumiendo se invoca un método definido en la clase del _CTX o uno de los comando de posprocesamiento de Abstract*/
							await Interpreter.Invoke(_CTX);
						}

						/*Posprocesamiento de valores de respuesta*/
						await Interpreter.PosProccess(_CTX);
					}
					else
					{
						Tools.ConsoleWrite(_CTX, "No se ha enviado ningún comando", true, ConsoleColor.Yellow);
						Tools.ConsolePrompt(_CTX);
					}
				}
			}
		}
	}
}