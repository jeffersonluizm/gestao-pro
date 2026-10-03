# Registro de Testes de Sistema

## O que são Testes de Sistema?

Testes de sistema validam o comportamento completo da aplicação em um ambiente que simula o uso real, verificando que todas as funcionalidades atendem aos requisitos especificados. No contexto do GestaoPro, os testes de sistema foram realizados manualmente, percorrendo os fluxos completos da aplicação (API + Web + Mobile) para garantir que o sistema funciona de ponta a ponta.

## Ambiente de Teste

| Componente | Configuração |
|------------|-------------|
| API Backend | ASP.NET Core 10, executando localmente na porta 5000 |
| Banco de Dados | MongoDB Atlas (cluster em nuvem) |
| Painel Web | React + Vite, executando na porta 3000 |
| App Mobile | React Native + Expo, executando via Expo Go |
| Navegador | Google Chrome (última versão) |
| Dispositivo Mobile | Smartphone Android via Expo Go |

## Cenários de Teste

### CT-001 — Fluxo de Autenticação Completo (RF-004, RF-005)

| Item | Descrição |
|------|-----------|
| **Objetivo** | Verificar que o login funciona para moradores (matrícula + senha) e administradores (e-mail + senha) |
| **Pré-condição** | Usuário cadastrado no sistema com credenciais válidas |
| **Passos** | 1. Acessar tela de login (web ou mobile) → 2. Inserir matrícula/e-mail e senha → 3. Clicar em "Entrar" |
| **Resultado Esperado** | Sistema redireciona para a tela principal com token JWT válido |
| **Resultado Obtido** | Login realizado com sucesso. Token JWT gerado e armazenado. Redirecionamento correto para dashboard |
| **Status** | **Aprovado** |

### CT-002 — Login com Credenciais Inválidas (RF-004, RF-005)

| Item | Descrição |
|------|-----------|
| **Objetivo** | Verificar que o sistema rejeita credenciais inválidas |
| **Pré-condição** | Nenhuma |
| **Passos** | 1. Acessar tela de login → 2. Inserir matrícula/e-mail inválido ou senha incorreta → 3. Clicar em "Entrar" |
| **Resultado Esperado** | Sistema exibe mensagem de erro e não permite acesso |
| **Resultado Obtido** | Retorno 401 Unauthorized. Mensagem de erro exibida corretamente |
| **Status** | **Aprovado** |

### CT-003 — Cadastro e Gerenciamento de Reservas (RF-007, RF-008, RF-009)

| Item | Descrição |
|------|-----------|
| **Objetivo** | Verificar fluxo completo de reserva de área comum |
| **Pré-condição** | Morador autenticado. Área comum cadastrada e ativa |
| **Passos** | 1. Acessar tela de reservas → 2. Selecionar área comum → 3. Escolher data e horário → 4. Confirmar reserva |
| **Resultado Esperado** | Reserva criada com status "pendente". Visível na listagem de reservas |
| **Resultado Obtido** | Reserva criada com sucesso. Status "pendente" atribuído. Validação de conflito de horário funcionando |
| **Status** | **Aprovado** |

### CT-004 — Conflito de Horário em Reservas (RF-007)

| Item | Descrição |
|------|-----------|
| **Objetivo** | Verificar que o sistema impede reservas com conflito de horário |
| **Pré-condição** | Reserva existente para a mesma área e data |
| **Passos** | 1. Tentar criar reserva no mesmo horário de uma reserva existente |
| **Resultado Esperado** | Sistema rejeita a reserva e exibe mensagem de conflito |
| **Resultado Obtido** | Reserva rejeitada com mensagem informando o conflito |
| **Status** | **Aprovado** |

### CT-005 — Publicação e Visualização de Avisos (RF-010, RF-012)

| Item | Descrição |
|------|-----------|
| **Objetivo** | Verificar que administradores podem publicar avisos e moradores podem visualizá-los |
| **Pré-condição** | Administrador autenticado |
| **Passos** | 1. Admin publica aviso via painel web → 2. Morador acessa app mobile → 3. Morador visualiza aviso |
| **Resultado Esperado** | Aviso publicado aparece na listagem do morador |
| **Resultado Obtido** | Aviso criado com sucesso. Visível no app mobile imediatamente após publicação |
| **Status** | **Aprovado** |

### CT-006 — Criação e Votação em Assembleia (RF-024, RF-025)

| Item | Descrição |
|------|-----------|
| **Objetivo** | Verificar fluxo completo de criação de assembleia e votação |
| **Pré-condição** | Administrador autenticado. Moradores cadastrados |
| **Passos** | 1. Admin cria assembleia com tópicos via painel web → 2. Morador acessa assembleia no app mobile → 3. Morador vota em um tópico |
| **Resultado Esperado** | Assembleia criada. Voto registrado. Resultado atualizado |
| **Resultado Obtido** | Assembleia criada com status "agendada". Votação registrada. Um voto por morador por tópico garantido |
| **Status** | **Aprovado** |

### CT-007 — Proteção contra Voto Duplicado (RF-025)

| Item | Descrição |
|------|-----------|
| **Objetivo** | Verificar que o sistema impede voto duplicado |
| **Pré-condição** | Morador já votou em um tópico |
| **Passos** | 1. Morador tenta votar novamente no mesmo tópico |
| **Resultado Esperado** | Sistema retorna erro 409 (conflito) |
| **Resultado Obtido** | Voto duplicado rejeitado com status 409. Mensagem informativa exibida |
| **Status** | **Aprovado** |

### CT-008 — Cadastro de Cobranças e Visualização de Boletos (RF-031, RF-032)

| Item | Descrição |
|------|-----------|
| **Objetivo** | Verificar fluxo de geração de cobrança e visualização pelo morador |
| **Pré-condição** | Administrador autenticado |
| **Passos** | 1. Admin cadastra cobrança → 2. Morador acessa tela financeira no app → 3. Morador visualiza boleto |
| **Resultado Esperado** | Cobrança criada com status "pending". Boleto visível para o morador |
| **Resultado Obtido** | Cobrança criada com código de barras gerado automaticamente. Boleto visível no app do morador |
| **Status** | **Aprovado** |

### CT-009 — Registro de Visitantes (RF-022, RF-023)

| Item | Descrição |
|------|-----------|
| **Objetivo** | Verificar registro de entrada e saída de visitantes |
| **Pré-condição** | Administrador autenticado |
| **Passos** | 1. Admin registra visitante com nome, documento e unidade → 2. Admin registra saída do visitante |
| **Resultado Esperado** | Visitante registrado com status "dentro". Após saída, status atualizado para "saiu" |
| **Resultado Obtido** | Fluxo completo funcionando. Timestamps de entrada e saída registrados corretamente |
| **Status** | **Aprovado** |

### CT-010 — Cadastro de Pets (RF-029, RF-030)

| Item | Descrição |
|------|-----------|
| **Objetivo** | Verificar cadastro e visualização de pets associados a unidades |
| **Pré-condição** | Morador autenticado com unidade associada |
| **Passos** | 1. Acessar tela de pets → 2. Cadastrar pet com nome, espécie, raça → 3. Visualizar pets cadastrados |
| **Resultado Esperado** | Pet cadastrado e visível na listagem da unidade |
| **Resultado Obtido** | Pet cadastrado com sucesso. Associação com unidade e morador funcionando corretamente |
| **Status** | **Aprovado** |

### CT-011 — Responsividade do Painel Web (RNF-001)

| Item | Descrição |
|------|-----------|
| **Objetivo** | Verificar que o painel web se adapta a diferentes tamanhos de tela |
| **Pré-condição** | Painel web acessível |
| **Passos** | 1. Acessar painel em desktop (1920px) → 2. Redimensionar para tablet (768px) → 3. Redimensionar para mobile (375px) |
| **Resultado Esperado** | Layout se adapta sem quebras ou sobreposições |
| **Resultado Obtido** | Layout responsivo funcionando. Elementos se reorganizam conforme a largura da viewport |
| **Status** | **Aprovado** |

### CT-012 — Alteração de Senha na Primeira Autenticação (RF-004)

| Item | Descrição |
|------|-----------|
| **Objetivo** | Verificar que o sistema exige troca de senha no primeiro login |
| **Pré-condição** | Usuário recém-cadastrado com flag `mustChangePassword` ativo |
| **Passos** | 1. Fazer login com credenciais iniciais → 2. Sistema redireciona para tela de alteração de senha → 3. Usuário define nova senha |
| **Resultado Esperado** | Sistema exige troca de senha antes de permitir acesso às funcionalidades |
| **Resultado Obtido** | Redirecionamento para alteração de senha funcionando. Nova senha sincronizada entre User e Credential |
| **Status** | **Aprovado** |

## Resumo dos Testes de Sistema

| Total de Cenários | Aprovados | Reprovados |
|-------------------|-----------|------------|
| 12 | 12 | 0 |

Todos os cenários de teste de sistema foram aprovados, validando que o GestaoPro atende aos requisitos funcionais e não funcionais especificados.
