using System;
using System.Linq;
using System.Threading.Tasks;

namespace NeoConsole
{

	internal class Program
	{
		static async Task Main(string[] args)
		{
			/*Instancia clase de control*/
			Context _CTX = new Context("Test", new Test());

			/*Muestra ayuda por default*/
			Tools.ConsoleWrite(Tools.Help(_CTX).ToString(), true, null);

			/*Muestra el prompt*/
			_CTX.ConsolePrompt();

			bool _eval = true;
			while (_eval)
			{
				/*Lee valor enviado desde la consola, es lo PRIMERO A HACER*/
				_CTX.Input = Console.ReadLine();

				/*Continua o sale del bucle de acuerdo a lo entregado por el Exec*/
				switch (Tools.Exec(_CTX))
				{
					case "break":
						_eval = false;
						break;
					case "continue":
						break;
					case "skip":
						/*Si continua agrega la data enviada desde la consola como línea*/
						_CTX.bufferCode.AppendLine(_CTX.Input);

						/* Contamos llaves para saber si el bloque está completo*/
						_CTX.indent += (_CTX.Input.Count(f => f == '{') - _CTX.Input.Count(f => f == '}'));

						/*Si no hay bloques abiertos, ejecutamos*/
						if (_CTX.indent <= 0)
						{
							/*Se ejecutan las acciones de control y asignación previas al intento de ejecución
							 *ES FUNDAMENTAL _State, ya que contiene todo el analisis previo y asignación del contexto */
							await Interpreter.Evaluate(_CTX);
						}
						break;
				}
			}
		}
	}
}