using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Acme.Sistemas.HomeFinance.App.Services;

/// <summary>
/// Cliente HTTP da Api. Injeta o token da sessão, serializa/desserializa em JSON camelCase e
/// traduz respostas de erro em <see cref="ErroApi"/>.
/// </summary>
public sealed class ApiClient(HttpClient http, ISessao sessao, ContextoTenant contextoTenant)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<T> GetAsync<T>(string caminho, CancellationToken ct = default) =>
        EnviarAsync<T>(HttpMethod.Get, caminho, null, ct);

    public Task<T> PostAsync<T>(string caminho, object? corpo, CancellationToken ct = default) =>
        EnviarAsync<T>(HttpMethod.Post, caminho, corpo, ct);

    /// <summary>POST sem corpo de resposta (204/200 ignorado).</summary>
    public async Task PostAsync(string caminho, object? corpo, CancellationToken ct = default) =>
        _ = await EnviarAsync<Vazio>(HttpMethod.Post, caminho, corpo, ct);

    public Task<T> PutAsync<T>(string caminho, object? corpo, CancellationToken ct = default) =>
        EnviarAsync<T>(HttpMethod.Put, caminho, corpo, ct);

    public Task<T> DeleteAsync<T>(string caminho, CancellationToken ct = default) =>
        EnviarAsync<T>(HttpMethod.Delete, caminho, null, ct);

    public async Task DeleteAsync(string caminho, CancellationToken ct = default) =>
        _ = await EnviarAsync<Vazio>(HttpMethod.Delete, caminho, null, ct);

    /// <summary>Baixa um arquivo: devolve os bytes e o nome sugerido pelo servidor.</summary>
    public async Task<(byte[] Conteudo, string Nome)> GetArquivoAsync(string caminho, string nomePadrao, CancellationToken ct = default)
    {
        using var resposta = await EnviarCoreAsync(HttpMethod.Get, caminho, null, ct);

        if (!resposta.IsSuccessStatusCode)
        {
            throw await LerErroAsync(resposta, ct);
        }

        var nome = resposta.Content.Headers.ContentDisposition?.FileNameStar
			?? resposta.Content.Headers.ContentDisposition?.FileName
			?? nomePadrao;

        var bytes = await resposta.Content.ReadAsByteArrayAsync(ct);
        return (bytes, nome.Trim('"'));
    }

    private sealed record Vazio;

    /// <summary>
    /// POST que distingue sucesso de conflito (409) com corpos de tipos diferentes — o caso da
    /// possível duplicidade de lançamento, cujo 409 traz um payload próprio, não o erro padrão.
    /// </summary>
    public async Task<(TOk? Ok, TConflito? Conflito)> PostOuConflitoAsync<TOk, TConflito>(
        string caminho,
        object corpo,
        CancellationToken ct = default)
        where TOk : class
        where TConflito : class
    {
        using var resposta = await EnviarCoreAsync(HttpMethod.Post, caminho, corpo, ct);

        if (resposta.StatusCode == HttpStatusCode.Conflict)
        {
            return (null, await resposta.Content.ReadFromJsonAsync<TConflito>(Json, ct));
        }

        if (!resposta.IsSuccessStatusCode)
        {
            throw await LerErroAsync(resposta, ct);
        }

        return (await resposta.Content.ReadFromJsonAsync<TOk>(Json, ct), null);
    }

    private async Task<T> EnviarAsync<T>(
        HttpMethod metodo,
        string caminho,
        object? corpo,
        CancellationToken ct)
    {
        using var resposta = await EnviarCoreAsync(metodo, caminho, corpo, ct);

        if (!resposta.IsSuccessStatusCode)
        {
            throw await LerErroAsync(resposta, ct);
        }

        if (resposta.StatusCode == HttpStatusCode.NoContent || typeof(T) == typeof(Vazio))
        {
            return default!;
        }

        var conteudo = await resposta.Content.ReadFromJsonAsync<T>(Json, ct);
        return conteudo ?? throw new ErroApi((int)resposta.StatusCode, [], "Resposta vazia da Api.");
    }

    private async Task<HttpResponseMessage> EnviarCoreAsync(
        HttpMethod metodo,
        string caminho,
        object? corpo,
        CancellationToken ct)
    {
        using var requisicao = new HttpRequestMessage(metodo, caminho);

        var token = await sessao.TokenAsync();
        if (token is not null)
        {
            requisicao.Headers.Authorization = new("Bearer", token);
        }

        // Tenant ativo selecionado: vai no cabeçalho que o middleware da Api resolve. Sem ele, a
        // Api usa o tenant padrão do token.
        if (contextoTenant.TenantId is { } tenantId)
        {
            requisicao.Headers.Add(ContextoTenant.CabecalhoTenant, tenantId.ToString());
        }

        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo, options: Json);
        }

        var resposta = await http.SendAsync(requisicao, ct);

        // Sessão expirada ou credencial recusada: derruba o token para a UI voltar ao login.
        if (resposta.StatusCode == HttpStatusCode.Unauthorized)
        {
            resposta.Dispose();
            await sessao.LimparAsync();
            throw new ErroApi(401, [], "Sessão expirada. Entre novamente.");
        }

        return resposta;
    }

    private static async Task<ErroApi> LerErroAsync(HttpResponseMessage resposta, CancellationToken ct)
    {
        var status = (int)resposta.StatusCode;
        try
        {
            var corpo = await resposta.Content.ReadFromJsonAsync<CorpoErro>(Json, ct);
            var erros = corpo?.Erros ?? [];
            var mensagem = erros.Count > 0 ? erros[0].Mensagem : $"Falha na requisição ({status}).";
            return new ErroApi(status, [.. erros], mensagem);
        }
        catch
        {
            return new ErroApi(status, [], $"Falha na requisição ({status}).");
        }
    }

    private sealed record CorpoErro(List<ErroDominio> Erros);
}
