## Purpose

Dá à operação do produto uma identidade própria, separada da do usuário final, capaz de
administrar contas sem enxergar os dados financeiros dos clientes.

## ADDED Requirements

### Requirement: Identidade administrativa separada

O sistema SHALL manter a identidade de administrador separada da identidade de usuário
final, e credenciais de um tipo SHALL NOT autenticar nos endpoints do outro.

#### Scenario: Administrador autentica no back-office
- **WHEN** um administrador informa credenciais válidas nos endpoints administrativos
- **THEN** o sistema o autentica com perfil administrativo

#### Scenario: Credencial de administrador em endpoint de usuário final
- **WHEN** uma credencial administrativa é usada em um endpoint de usuário final
- **THEN** o sistema recusa a autenticação

#### Scenario: Credencial de usuário final em endpoint administrativo
- **WHEN** uma credencial de usuário final é usada em um endpoint administrativo
- **THEN** o sistema recusa o acesso

### Requirement: Administrador não possui tenant

O sistema SHALL NOT vincular tenant a uma identidade administrativa, e requisições
administrativas SHALL NOT ter tenant ativo definido.

#### Scenario: Requisição administrativa sem tenant ativo
- **WHEN** o administrador executa uma requisição autenticada
- **THEN** nenhum tenant é definido como ativo para aquela requisição

### Requirement: Administrador não acessa dado financeiro

O sistema SHALL impedir que identidade administrativa leia lançamentos, orçamentos,
alertas, regras de classificação ou relatórios de qualquer cliente.

#### Scenario: Consulta administrativa a tabela de dado financeiro
- **WHEN** uma requisição autenticada como administrador consulta lançamentos de clientes
- **THEN** nenhuma linha é retornada

#### Scenario: Acesso a relatório de cliente
- **WHEN** um administrador tenta abrir o relatório mensal de um cliente
- **THEN** o sistema recusa o acesso

### Requirement: Consulta administrativa de contas

O administrador SHALL poder consultar a relação de contas com dados de identificação e
situação, sem qualquer valor financeiro.

#### Scenario: Listagem de contas
- **WHEN** o administrador abre a relação de contas
- **THEN** o sistema apresenta identificação, data de criação e situação de cada conta,
  sem exibir valores de lançamento, renda, orçamento ou saldo

#### Scenario: Conta excluída
- **WHEN** uma conta foi excluída pelo titular
- **THEN** ela deixa de aparecer na relação administrativa
