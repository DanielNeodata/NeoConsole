using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeoConsole
{

	internal class Program
	{
		static async Task Main(string[] args)
		{
			/*Instancia clase de control*/
			Context _CTX = new Context("Test", "NeoConsole.Test");

			/*Muestra ayuda por default*/
			Tools.ConsoleWrite(Tools.Help(_CTX).ToString(), true, null);

			/*Muestra el prompt*/
			_CTX.ConsolePrompt();

			while (true)
			{
				/*Lee valor enviado desde la consola*/
				_CTX.Input = Console.ReadLine();

				/*Muestra el prompt*/
				_CTX.ConsolePrompt();

				/*Continua o sale del bucle de acurdo a lo entregado por el Exec*/
				if (!Tools.Exec(_CTX)) { break; }

				/*Si continua agrega la data enviada desde la consola como línea*/
				_CTX.bufferCode.AppendLine(_CTX.Input);

				/* Contamos llaves para saber si el bloque está completo*/
				_CTX.indent += (_CTX.Input.Count(f => f == '{') - _CTX.Input.Count(f => f == '}'));

				/*Si no hay bloques abiertos, ejecutamos*/
				if (_CTX.indent <= 0)
				{
					/*Se ejecutan las acciones de control y asignación previas al intento de ejecución
					 *ES FUNDAMENTAL _State, ya que contiene todo el analisis previo y asignación del contexto */
					Interpreter.Evaluate(_CTX);
				}
			}
		}
	}
}