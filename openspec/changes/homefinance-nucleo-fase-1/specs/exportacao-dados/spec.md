## Purpose

Permite gerar um arquivo com os dados do próprio tenant em diferentes formatos, desligado
por padrão e habilitado conta a conta pela operação quando o titular formaliza uma
requisição de acesso.

## ADDED Requirements

### Requirement: Exportação desabilitada por padrão

O sistema SHALL manter o recurso de exportação desligado por padrão, controlado por
feature flag, e SHALL NOT oferecê-lo a nenhuma conta enquanto não for habilitado.

#### Scenario: Recurso não habilitado
- **WHEN** o usuário abre o aplicativo com a exportação não habilitada para sua conta
- **THEN** o recurso não é apresentado e nenhuma exportação pode ser iniciada

#### Scenario: Tentativa de exportar sem habilitação
- **WHEN** uma requisição de exportação chega para uma conta sem o recurso habilitado
- **THEN** o sistema a recusa

### Requirement: Habilitação por conta pela administração

A habilitação e a desabilitação da exportação de uma conta SHALL ocorrer exclusivamente
por ação de administrador autenticado no portal administrativo, com motivo informado, e
SHALL NOT haver qualquer outro caminho de habilitação.

#### Scenario: Habilitação após requisição do titular
- **WHEN** o administrador habilita a exportação para uma conta pelo portal, informando o
  motivo
- **THEN** a conta passa a ter o recurso disponível e a habilitação fica registrada com
  autor, data e motivo

#### Scenario: Habilitação sem motivo
- **WHEN** o administrador tenta habilitar a exportação sem informar o motivo
- **THEN** o sistema recusa a habilitação

#### Scenario: Aplicativo não habilita o recurso
- **WHEN** uma requisição vinda do aplicativo do usuário final tenta habilitar a exportação
- **THEN** o sistema a recusa, por a habilitação existir apenas no portal administrativo

#### Scenario: Desabilitação
- **WHEN** a administração desabilita a exportação de uma conta
- **THEN** o recurso deixa de ser oferecido e exportações não concluídas são canceladas

#### Scenario: Habilitação não concede leitura ao administrador
- **WHEN** a administração habilita a exportação de uma conta
- **THEN** o administrador continua sem acesso aos dados financeiros daquela conta

### Requirement: Aviso ao titular

O sistema SHALL avisar o titular quando a exportação for habilitada para sua conta.

#### Scenario: Aviso na habilitação
- **WHEN** a exportação é habilitada para a conta da Ana
- **THEN** Ana é avisada de que o recurso está disponível

#### Scenario: Aviso independe da situação da assinatura
- **WHEN** a exportação é habilitada para uma conta com assinatura suspensa
- **THEN** o aviso é entregue mesmo com as demais notificações suspensas

### Requirement: Habilitação prevalece sobre a restrição de assinatura

Quando a exportação estiver habilitada para uma conta, o sistema SHALL permitir seu uso
independentemente da situação da assinatura.

#### Scenario: Exportação com assinatura suspensa
- **WHEN** a conta tem exportação habilitada e assinatura suspensa e o titular solicita a
  exportação
- **THEN** o sistema gera o arquivo normalmente

### Requirement: Formatos de exportação

O sistema SHALL oferecer a exportação em mais de um formato de arquivo, incluindo ao menos
um formato de leitura humana e um formato de dados estruturados, com o titular escolhendo
o formato na solicitação.

#### Scenario: Formato de leitura escolhido
- **WHEN** o titular solicita a exportação em formato de documento para leitura
- **THEN** o arquivo é gerado nesse formato

#### Scenario: Formato estruturado escolhido
- **WHEN** o titular solicita a exportação em formato de dados estruturados
- **THEN** o arquivo é gerado nesse formato, com os lançamentos em linhas e colunas

#### Scenario: Formato não suportado
- **WHEN** o titular solicita um formato que o sistema não oferece
- **THEN** o sistema recusa a solicitação e informa os formatos disponíveis

### Requirement: Escopo restrito ao próprio tenant

A exportação SHALL conter exclusivamente os dados do tenant do próprio titular, e SHALL
NOT incluir dados de tenant recebido por compartilhamento.

#### Scenario: Titular exporta o próprio tenant
- **WHEN** Ana, com o recurso habilitado, solicita a exportação
- **THEN** o arquivo contém apenas os dados do tenant dela

#### Scenario: Destinatário de compartilhamento tenta exportar
- **WHEN** Bruno, com acesso somente leitura ao tenant da Ana, solicita a exportação
  daquele tenant
- **THEN** o sistema recusa a solicitação

### Requirement: Geração assíncrona com arquivo temporário

O sistema SHALL gerar a exportação de forma assíncrona, avisar o titular quando o arquivo
estiver pronto e manter o arquivo disponível por prazo determinado, removendo-o ao fim
desse prazo.

#### Scenario: Arquivo pronto
- **WHEN** a geração é concluída
- **THEN** o titular é avisado e o arquivo fica disponível para download

#### Scenario: Prazo expirado
- **WHEN** o prazo de disponibilidade do arquivo se encerra
- **THEN** o arquivo é removido do armazenamento e o download deixa de funcionar

#### Scenario: Download por outra conta
- **WHEN** uma conta diferente da que solicitou tenta baixar o arquivo
- **THEN** o sistema recusa o download

### Requirement: Registro em auditoria

O sistema SHALL registrar em auditoria a habilitação e a desabilitação do recurso, cada
solicitação de exportação e cada download realizado.

#### Scenario: Exportação auditada
- **WHEN** uma exportação é solicitada e baixada
- **THEN** o sistema registra autor, data, formato e evento de download
