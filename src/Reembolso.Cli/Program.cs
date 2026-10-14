using System.Text;
using Reembolso.Cli;

// Ponto de entrada: só prepara stdin/stdout/stderr em UTF-8 (para não perder
// os acentos de "Consulta Médica") e delega todo o trabalho para a Aplicacao.
var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

using var entrada = new StreamReader(Console.OpenStandardInput(), utf8);
using var saida = new StreamWriter(Console.OpenStandardOutput(), utf8);
using var erros = new StreamWriter(Console.OpenStandardError(), utf8);

return Aplicacao.Executar(args, entrada, saida, erros, DateOnly.FromDateTime(DateTime.Today));
