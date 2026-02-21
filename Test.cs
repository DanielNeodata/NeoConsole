using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace NeoConsole
{
	public class Test: Abstract
	{
		public string fBuildJson()
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
