using Microsoft.Extensions.Configuration;
using NeoConsole.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NeoConsole
{

	internal static class Program
	{
		internal static Dictionary<string, Context> _ALL = new Dictionary<string, Context>();
		internal static Dictionary<string, Info> infoPrefixs = new Dictionary<string, Info>();
		internal static Dictionary<string, Info> infoContexts = new Dictionary<string, Info>();
		internal static Context _CTX = null;
		internal static IConfiguration config = null;
		internal static string _preContext = "[ctx]";
		internal static string _preCommand = "[run]";

		static async Task Main(string[] args)
		{
			/*Llamada a inicialización de la consola*/
			await Initialize();

			/*Loop mientras la consola está activa*/
			while (true)
			{
				/*Lee el input del usuario*/
				string _input = Console.ReadLine();

				/*Evalúa si el input empieza con preContext*/
				if (_input.ToLower().StartsWith(_preContext))
				{
					/*Quita el prefijo*/
					_input = _input.Substring(_preContext.Length);

					if (_ALL.ContainsKey(_input))
					{
						/*Cambio de contexto según envío del usuario*/
						_CTX = _ALL[_input];

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
					/*Asigna input del usuario al contexto activo*/
					_CTX.Input = _input;

					/*Evalúa si nos e ha enviado ningún input significativo*/
					if (!string.IsNullOrWhiteSpace(_CTX.Input))
					{
						/*Evalúa si el input empieza con preCommand*/
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
		static async Task Initialize()
		{
			/*Lectura de la configuración externa */
			config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();

			/*-----------------------------------------------------------------------------*/
			/*Definición de info de prefijos de ejecución y contextos para mostrar en ayuda*/
			/*-----------------------------------------------------------------------------*/
			IConfigurationSection _cfgContexts = config.GetSection("Contexts");
			foreach (IConfigurationSection s in _cfgContexts.GetChildren())
			{
				string[] _item = s.Value.Split('|');
				infoContexts.Add(s.Key, new Info() { Type = "context", Key = _item[0], Description = _item[1], ClassName = _item[2] });
			}

			infoPrefixs.Add("context", new Info() { Type = "command", Key = "[ctx]", Description = "Cambiar el contexto", Example = "[ctx]Contexto" });
			infoPrefixs.Add("run", new Info() { Type = "command", Key = "[run]", Description = "Crear funciones en el contexto activo", Example = "[run]int fncname(string a, int b)" });
			/*-----------------------------------------------------------------------------*/

			/*-----------------------------------------------------------------------------*/
			/*Carga de los contextos disponibles*/
			/*-----------------------------------------------------------------------------*/
			foreach (KeyValuePair<string, Info> entry in infoContexts)
			{
				_ALL.Add(entry.Value.Key, new Context(entry.Value.Key, entry.Value.ClassName, entry.Value.Description, infoPrefixs, infoContexts));
			}
			/*-----------------------------------------------------------------------------*/

			/*Contexto por default*/
			_CTX = _ALL["Test"];

			/*Muestra ayuda por default*/
			Tools.ConsoleWrite(_CTX, Tools.Help(_CTX).ToString(), true, null);

			/*Muestra prompt por default*/
			Tools.ConsolePrompt(_CTX);
		}
	}
}