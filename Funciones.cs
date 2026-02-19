using System;
using System.Diagnostics;
using System.Text;

namespace NeoConsole
{
    public class Funciones
    {
        /*
        public string Saludar(string nombre)
        {
            return $"Hola, {nombre}!";
        }

        public int Sumar(int a, int b)
        {
            return a + b;
        }

        public double AreaCirculo(double radio)
        {
            return Math.PI * Math.Pow(radio, 2);
        }
        */

        public string Check(string tipo, int totalRegistros)
        {
            Stopwatch sw = new Stopwatch();
            string msg = "";
            string resultado = "";
            string cadena1 = "123456";
            string cadena2 = "Juan Perez";
            string cadena3 = "Consulta Técnica";
            StringBuilder sb = new StringBuilder(32768);

            sw.Start();
            for (int i = 0; i < totalRegistros; i++)
            {
                switch (tipo)
                {
                    case "1":
                        // --- PRUEBA 1: Bloque 1 (string +=) ---
                        resultado += "Cuenta: 123456";
                        resultado += Environment.NewLine;
                        resultado += "Usuario: Juan Perez";
                        resultado += Environment.NewLine;
                        resultado += "Motivo: Consulta Técnica";
                        resultado += Environment.NewLine;
                        msg = "+=";
                        break;
                    case "2":
                        // --- PRUEBA 2: Bloque 2 (string Concat) ---
                        resultado = string.Concat(resultado, @"Cuenta: 123456\n\r", @"Usuario: Juan Perez\n\r", @"Motivo: Consulta Técnica\n\r");
                        msg = "Concat";
                        break;
                    case "3":
                        // --- PRUEBA 3: Bloque 3 (StringBuilder) ---
                        sb.AppendLine("Cuenta: 123456");
                        sb.AppendLine("Usuario: Juan Perez");
                        sb.AppendLine("Motivo: Consulta Técnica");
                        msg = "StringBuilder";
                        break;
                    case "4":
                        // --- PRUEBA 4: Bloque 4 (StringBuilder + Concat) ---
                        sb.AppendLine(string.Concat("Cuenta: 123456", Environment.NewLine, "Usuario: Juan Perez", Environment.NewLine, "Motivo: Consulta Técnica"));
                        msg = "StringBuilder + Concat";
                        break;
                    case "5":
                        // --- PRUEBA 5: Bloque 5 (StringBuilder + Interpolacion) ---
                        sb.AppendLine($"Cuenta: {cadena1}{Environment.NewLine}Usuario: {cadena2}{Environment.NewLine}Motivo: {cadena3}");
                        msg = "StringBuilder + Interpolacion";
                        break;
                }
            }
            sw.Stop();
            string result = $"Tiempo Bloque {tipo} (string {msg}): {sw.ElapsedMilliseconds} ms";

            sw.Reset();

            return result;
        }

        public string TestCode()
        {
            string Documento = "17634250";
            string Nombre = "Ruben";
            string Sexo = "M";
            string FormatReport = "No";
            string _sector = "Central";
            string _billTo = "Main";
            string _shipTo = "Casa";
            string _configuration = "Pepe=5";
            string _customer = "Yo";
            string _model = "Fluence";

            StringBuilder _rawbody1 = new StringBuilder();
            _rawbody1.Append("{\"applicants\": {\"primaryConsumer\": {\"personalInformation\": {\"entity\": {\"consumer\": {\"names\": ");
            _rawbody1.Append("[{\"data\": {\"documento\": \"" + Documento + "\",\"nombre\": \"" + Nombre + "\",\"sexo\": \"" + Sexo + "\"}}],");
            _rawbody1.Append(" \"identifiers\": { },\"dob\": { },\"addresses\": [],\"phones\": {\"trabajo\": \"\"},\"legacyIds\": {\"argId\": \"\"},\"emails\": {\"oficina\": \"\"},\"discoveryData\": { },\"fileSinceDate\": {\"inicioDate\": \"\"},\"origin\": { }}},");
            _rawbody1.Append(" \"productData\": {");
            _rawbody1.Append(" \"sector\": \"" + _sector + "\",\"billTo\": \"" + _billTo + "\",\"shipTo\": \"" + _shipTo + "\",\"formatReport\": \"" + FormatReport + "\",");
            _rawbody1.Append(" \"configuration\":\"" + _configuration + "\",");
            _rawbody1.Append(" \"customer\":\"" + _customer + "\",");
            _rawbody1.Append(" \"model\":\"" + _model + "\",");
            _rawbody1.Append(" \"producto\":\"RISC:experto\"},");
            _rawbody1.Append(" \"clientConfig\": { \"clientTxId\": \"0\",\"clientReference\": \"\"},\"configData\": { },\"variables\": { },\"globalVariables\": { },\"vinculos\": { }}}}}");


            StringBuilder _rawbody2 = new StringBuilder();
            _rawbody2.Append("{\"applicants\": {\"primaryConsumer\": {\"personalInformation\": {\"entity\": {\"consumer\": {\"names\": ");
            _rawbody2.Append($"[{{\"data\": {{\"documento\": \"{Documento}\",\"nombre\": \"{Nombre}\",\"sexo\": \"{Sexo}\"}}}}],");
            _rawbody2.Append($" \"identifiers\": {{ }},\"dob\": {{ }},\"addresses\": [],\"phones\": {{\"trabajo\": \"\"}},\"legacyIds\": {{\"argId\": \"\"}},\"emails\": {{\"oficina\": \"\"}},\"discoveryData\": {{ }},\"fileSinceDate\": {{\"inicioDate\": \"\"}},\"origin\": {{ }}}}}},");
            _rawbody2.Append(" \"productData\": {");
            _rawbody2.Append($" \"sector\": \"{_sector}\",\"billTo\": \"{_billTo}\",\"shipTo\": \"{_shipTo}\",\"formatReport\": \"{FormatReport}\",");
            _rawbody2.Append($" \"configuration\":\"{_configuration}\",");
            _rawbody2.Append($" \"customer\":\"{_customer}\",");
            _rawbody2.Append($" \"model\":\"{_model}\",");
            _rawbody2.Append(" \"producto\":\"RISC:experto\"},");
            _rawbody2.Append(" \"clientConfig\": { \"clientTxId\": \"0\",\"clientReference\": \"\"},\"configData\": { },\"variables\": { },\"globalVariables\": { },\"vinculos\": { }}}}}");

            return $"\n\r{_rawbody1.ToString()}\n\r{_rawbody2.ToString()}\n\r";

        }
    }
}
