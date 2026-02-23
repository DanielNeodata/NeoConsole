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
		internal static Dictionary<string, Info> infoSyntax = new Dictionary<string, Info>();
		internal static Dictionary<string, Info> infoContexts = new Dictionary<string, Info>();
		internal static Context _CTX = null;
		internal static IConfiguration config = null;
		internal static string _Command = "";

		static async Task Main(string[] args)
		{
			/*Llamada a inicialización de la consola*/
			await Initialize();

			/*Loop mientras la consola está activa*/
			while (true)
			{
				/*Asigna input del usuario al contexto activo*/
				_CTX.Input = Console.ReadLine();

				/*Evalua si se debe ejecutar lo enviado como parte del un preCommand
				 * Si el EvalInput() es false, debe ejecutar el Invoke, asumiendo es una funcion definida en el stack
				 */
				_Command = await Interpreter.EvalInput(_CTX);
				if (_Command == "")
				{
					/*Llamada asumiendo se invoca un método definido en la clase del _CTX o uno de los comando de posprocesamiento de Abstract*/
					await Interpreter.Invoke(_CTX);
				}

				/*Posprocesamiento de valores de respuesta*/
				await PosProccess();
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

			/*Carga de elementos sintácticos*/
			infoSyntax.Add("run", new Info() { Type = "command", Key = "[run]", Description = "Crear funciones en el contexto activo", Example = "[run]int fncname(string a, int b)" });
			/*-----------------------------------------------------------------------------*/

			/*-----------------------------------------------------------------------------*/
			/*Carga de los contextos disponibles*/
			/*-----------------------------------------------------------------------------*/
			foreach (KeyValuePair<string, Info> entry in infoContexts)
			{
				_ALL.Add(entry.Value.Key, new Context(entry.Value.Key, entry.Value.ClassName, entry.Value.Description, infoSyntax, infoContexts));
			}
			/*-----------------------------------------------------------------------------*/

			/*Contexto por default*/
			_CTX = _ALL["Test"];

			/*Muestra ayuda por default*/
			Tools.ConsoleWrite(_CTX, Tools.Help(_CTX).ToString(), true, null);

			/*Muestra prompt por default*/
			Tools.ConsolePrompt(_CTX);
		}

		public static async Task PosProccess()
		{
			try
			{
				if (_CTX.State!=null && _CTX.State.ReturnValue != null && _CTX.State.ReturnValue.ToString().StartsWith("do:"))
				{
					string[] _vals = _CTX.State.ReturnValue.ToString().Split(':');
					switch (_vals[1])
					{
						case "change":
							string _context = _vals[2].Replace("\"", "");
							if (_ALL.ContainsKey(_context) )
							{
								_CTX = _ALL[_context];
								Tools.ConsoleWrite(_CTX, Tools.Help(_CTX).ToString(), true, null);
								Tools.ConsoleWrite(_CTX, $"Se activó el contexto: {_context}", false, ConsoleColor.Green);
							}
							else
							{
								Tools.ConsoleWrite(_CTX, $"No se existe el contexto {_context}", true, ConsoleColor.Red);
							}
							break;
						case "clear":
							Tools.ConsoleClear();
							break;
						case "help":
							Tools.ConsoleWrite(_CTX, Tools.Help(_CTX).ToString(), true, null);
							break;
						case "exit":
							System.Environment.Exit(0);
							break;
					}
				}
				Tools.ConsolePrompt(_CTX);
			}
			catch (Exception ex)
			{
				Tools.ConsoleError(_CTX, ex);
			}
		}

	}
}