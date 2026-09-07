# C# Integrations

API de portfólio construída em .NET 9 para demonstrar padrões de integração, autenticação, serviços externos e recursos de plataforma. O projeto é organizado para crescer por módulos: cada nova integração deve ser adicionada sem acoplar as demais funcionalidades.

## Estrutura

```text
csharp-integrations.api    # Camada HTTP, controllers e configuração da aplicação
csharp-integrations.core   # Serviços, clientes de integração e regras reutilizáveis
csharp-integrations.tests  # Testes unitários e de integração
```

## Funcionalidades atuais

### Autenticação e autorização

- Login de demonstração com emissão de access token JWT.
- Validação de assinatura, emissor, audiência e expiração do JWT.
- Proteção de endpoints com `[Authorize]`.
- Integração SAML 2.0 opcional: só é registrada quando toda a configuração obrigatória está presente.
- Validação de `returnUrl` no fluxo SAML para impedir redirecionamentos externos não autorizados.
- Usuários em memória exclusivamente como seed de demonstração; não representam uma implementação de identidade para produção.

### Inteligência artificial

- Cliente para um servidor Ollama configurável.
- Consulta de saúde e listagem de modelos disponíveis.
- Download de modelos autenticado.
- Chat com resposta completa ou streaming.
- Validação dos contratos de entrada para prompts e modelos.

### Plataforma e segurança HTTP

- Rate limit para login, operações de IA e download de modelos.
- CORS opcional, ativado somente quando `Cors:AllowedOrigins` é configurado.
- Redirecionamento HTTPS.
- Respostas de erro RFC 7807 (`ProblemDetails`) com `traceId` para rastreabilidade.
- Tratamento global de exceções, sem expor detalhes internos fora do ambiente de desenvolvimento.
- Swagger UI em ambiente de desenvolvimento.
- Esquema HTTP Bearer no Swagger, aplicado apenas aos endpoints que exigem autenticação.
- Documentação XML exposta no Swagger.

### Qualidade e testes

- Testes unitários para geração de token, extração de claims e repositório de usuários de demonstração.
- Testes de integração executando a API em memória.
- Cobertura inicial de login, credenciais inválidas, endpoints protegidos e rate limit.
- Configuração determinística nos testes, sem depender de User Secrets ou de uma instância real do Ollama.

## Executar localmente

Pré-requisito: .NET SDK 9.

1. Configure os segredos de desenvolvimento da API. A chave de assinatura JWT nunca deve ser versionada.

```powershell
dotnet user-secrets set "BearerToken:ApiKey" "uma-chave-longa-e-segura" --project .\csharp-integrations.api
```

2. Restaure e execute a API.

```powershell
dotnet restore
dotnet run --project .\csharp-integrations.api
```

3. Execute os testes.

```powershell
dotnet test
```

Em desenvolvimento, o Swagger fica disponível na rota `/swagger` da URL exibida pela aplicação.

## Configuração

`Issuer` e `Audience` do JWT podem ficar no `appsettings.json`, pois são identificadores públicos do token. A chave `BearerToken:ApiKey` deve ficar em User Secrets no desenvolvimento e em um gerenciador de segredos no ambiente de produção.

Configurações de CORS, Ollama e SAML são opcionais conforme a integração utilizada. Para habilitar SAML, informe todos os campos obrigatórios da seção `SAML`; uma configuração parcial gera erro propositalmente, evitando um fluxo de autenticação incompleto.

## Expansão planejada

Este repositório foi pensado como uma vitrine incremental. As categorias abaixo serão preenchidas conforme novas features forem implementadas.

### Identidade

- Refresh tokens, rotação, revogação e logout.
- OAuth 2.0 e OpenID Connect.
- Provedores sociais e autenticação baseada em API keys.
- Persistência de usuários, hash de senha, papéis e policies.

### Serviços e integrações

- Clientes HTTP para APIs externas.
- Processamento e interpretação de formatos e protocolos.
- Webhooks, notificações e integrações de terceiros.
- Observabilidade, health checks e resiliência.

### Inteligência artificial

- Novos provedores e modelos de IA.
- Chat com histórico, respostas estruturadas e streaming.
- Embeddings, busca semântica e ferramentas para agentes.

### Dados

- Bancos relacionais e NoSQL.
- ORM, migrations, cache e repositórios.
- Estratégias de consistência, auditoria e isolamento de dados.

### Mensageria e eventos

- Filas, publishers e consumers.
- Eventos de domínio e integração.
- Retries, dead-letter queues e processamento idempotente.

## Princípios do projeto

- Integrações independentes, configuráveis e testáveis.
- Segredos fora do código e do controle de versão.
- Endpoints documentados e protegidos conforme o nível de acesso necessário.
- Evolução incremental, com testes acompanhando cada feature.
