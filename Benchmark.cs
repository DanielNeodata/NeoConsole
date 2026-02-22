using System;
using System.Diagnostics;
using System.Text;

namespace NeoConsole
{
	public class Benchmark : Abstract
	{
		Stopwatch sw = new Stopwatch();
		string msg = "";

		[CustomDescription("Test con +=")]
		public string fPlus(int totalRegistros)
		{
			string resultado = "";
			sw.Start();
			for (int i = 0; i < totalRegistros; i++)
			{
				resultado += "Cuenta: 123456";
				resultado += Environment.NewLine;
				resultado += "Usuario: Juan Perez";
				resultado += Environment.NewLine;
				resultado += "Motivo: Consulta Técnica";
				resultado += Environment.NewLine;
				msg = "+=";
			}
			sw.Stop();
			string result = $"Tiempo Bloque (string {msg}): {sw.ElapsedMilliseconds} ms";
			sw.Reset();
			return result;
		}
		[CustomDescription("Test con Concat()")]
		public string fConcat(int totalRegistros)
		{
			string resultado = "";
			sw.Start();
			for (int i = 0; i < totalRegistros; i++)
			{
				resultado = string.Concat(resultado, @"Cuenta: 123456\n\r", @"Usuario: Juan Perez\n\r", @"Motivo: Consulta Técnica\n\r");
				msg = "Concat";
			}
			sw.Stop();
			string result = $"Tiempo Bloque (string {msg}): {sw.ElapsedMilliseconds} ms";
			sw.Reset();
			return result;
		}
		[CustomDescription("Test con StringBuilder")]
		public string fStringBuilder(int totalRegistros)
		{
			StringBuilder sb = new StringBuilder();
			sw.Start();
			sb.AppendLine("Cuenta: 123456");
			sb.AppendLine("Usuario: Juan Perez");
			sb.AppendLine("Motivo: Consulta Técnica");
			msg = "StringBuilder";
			sw.Stop();
			string result = $"Tiempo Bloque (string {msg}): {sw.ElapsedMilliseconds} ms";
			sw.Reset();
			return result;
		}
		[CustomDescription("Test con StringBuilder + Concat()")]
		public string fStringBuilder_Concat(int totalRegistros)
		{
			string msg = "";
			StringBuilder sb = new StringBuilder();
			sw.Start();
			for (int i = 0; i < totalRegistros; i++)
			{
				sb.AppendLine(string.Concat("Cuenta: 123456", Environment.NewLine, "Usuario: Juan Perez", Environment.NewLine, "Motivo: Consulta Técnica"));
				msg = "StringBuilder + Concat";
			}
			sw.Stop();
			string result = $"Tiempo Bloque (string {msg}): {sw.ElapsedMilliseconds} ms";
			sw.Reset();
			return result;
		}
		[CustomDescription("Test con StringBuilder + Interpolación")]
		public string fStringBuilder_Interpolacion(int totalRegistros)
		{
			string cadena1 = "123456";
			string cadena2 = "Juan Perez";
			string cadena3 = "Consulta Técnica";
			StringBuilder sb = new StringBuilder();
			sw.Start();
			for (int i = 0; i < totalRegistros; i++)
			{
				sb.AppendLine($"Cuenta: {cadena1}{Environment.NewLine}Usuario: {cadena2}{Environment.NewLine}Motivo: {cadena3}");
				msg = "StringBuilder + Interpolacion";
			}
			sw.Stop();
			string result = $"Tiempo Bloque (string {msg}): {sw.ElapsedMilliseconds} ms";
			sw.Reset();
			return result;
		}
	}
}
