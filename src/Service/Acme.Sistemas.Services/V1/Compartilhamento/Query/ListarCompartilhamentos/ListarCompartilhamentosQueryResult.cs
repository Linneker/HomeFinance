namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.ListarCompartilhamentos;

/// <param name="DestinatarioId">Conta que recebeu o acesso — é por ela que a revogação é feita.</param>
public sealed record CompartilhamentoConcedido(Guid DestinatarioId, bool IncluiScr, DateTimeOffset ConcedidoEm);

public sealed record ListarCompartilhamentosQueryResult(IReadOnlyList<CompartilhamentoConcedido> Compartilhamentos);
