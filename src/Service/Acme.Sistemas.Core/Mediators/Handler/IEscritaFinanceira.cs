namespace Acme.Sistemas.Core.Mediators.Handler;

/// <summary>
/// Marca um Command como escrita de dado financeiro.
/// </summary>
/// <remarks>
/// É o que o pipeline usa para decidir onde o onboarding bloqueia. Marcar o Command, em vez de
/// listar nomes num arquivo de configuração, faz a decisão viajar junto do próprio Command: um
/// Command novo que esqueça a marca não é bloqueado, e isso aparece no code review do arquivo
/// que está sendo criado — não num arquivo distante que ninguém abriu.
/// <para>
/// Cadastrar renda não leva a marca de propósito: é justamente a ação que cumpre o requisito
/// bloqueante. Marcá-la trancaria a pessoa para fora do único caminho que a destrava.
/// </para>
/// </remarks>
public interface IEscritaFinanceira;
