# Template Padrão da Aplicação

<span style="color:red">Pré-requisitos: <a href="2-Especificação do Projeto.md"> Especificação do Projeto</a></span>, <a href="3-Projeto de Interface.md"> Projeto de Interface</a>, <a href="4-Metodologia.md"> Metodologia</a>

## Identidade Visual

O sistema GestaoPro utiliza uma paleta de cores baseada em tons de azul e branco, transmitindo confiança e profissionalismo. O logotipo é apresentado na tela de login e no menu lateral do painel web.

### Paleta de Cores

| Elemento | Cor | Código |
|----------|-----|--------|
| Primária (headers, botões de ação) | Azul | `#2196F3` |
| Fundo principal | Branco | `#FFFFFF` |
| Texto primário | Cinza escuro | `#333333` |
| Texto secundário | Cinza | `#666666` |
| Destaque / Sucesso | Verde | `#4CAF50` |
| Alerta / Erro | Vermelho | `#F44336` |

### Tipografia

- **Fonte principal (Web):** sistema padrão (`-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif`)
- **Fonte principal (Mobile):** padrão do sistema operacional (San Francisco no iOS, Roboto no Android)

## Layout — Painel Web (React + TypeScript + Vite)

O painel web é uma SPA (Single Page Application) voltada para administradores. A estrutura segue o padrão:

- **Tela de Login:** formulário centralizado com campos de e-mail/matrícula e senha
- **Painel principal:** layout com área de conteúdo central onde as funcionalidades são exibidas
- **Navegação:** acesso às seções de Usuários, Avisos, Reservas, Áreas Comuns, Assembleias, Financeiro, Ocorrências, Visitantes, Encomendas, Estacionamento e Pets

### Responsividade

O painel web é responsivo, adaptando-se a diferentes tamanhos de tela (desktop, tablet). Utiliza CSS com media queries para ajustar o layout conforme a largura da viewport.

## Layout — App Mobile (React Native + Expo)

O aplicativo mobile é voltado para moradores e segue o padrão de navegação por abas (Tab Bar) na parte inferior da tela.

### Estrutura de Navegação

- **Tab Bar inferior** com 5 abas principais + tela "Mais" para funcionalidades adicionais
- **Stack Navigation** dentro de cada aba para navegação hierárquica
- **Telas de autenticação** (Login e Cadastro) são exibidas antes do acesso ao conteúdo principal

### Telas Principais do App Mobile

| Tela | Descrição |
|------|-----------|
| LoginScreen | Tela de autenticação com matrícula e senha |
| RegisterScreen | Tela de cadastro de novo morador |
| HomeScreen | Dashboard com resumo das informações do condomínio |
| ReservationsScreen | Listagem e criação de reservas de áreas comuns |
| AreasComunsScreen | Visualização das áreas comuns disponíveis |
| AssembleiasScreen | Listagem de assembleias e acesso a votações |
| VotosScreen | Interface de votação em assembleias |
| EncomendasScreen | Acompanhamento de encomendas e correspondências |
| DeliveriesScreen | Confirmação de retirada de encomendas |
| OccurrencesScreen | Registro e acompanhamento de ocorrências |
| PetsScreen | Cadastro e visualização de pets |
| VisitantesScreen | Registro e controle de visitantes |
| ProfileScreen | Visualização e edição do perfil do morador |
| MaisScreen | Menu com funcionalidades adicionais |

### Responsividade Mobile

O app utiliza utilitários de responsividade customizados (`responsive.ts`) com funções `sw` (scale width), `sf` (scale font) e `HP` (height percentage) para garantir que as telas se adaptem a diferentes tamanhos de dispositivos (320dp a 430dp).

## Iconografia

- **Mobile:** Ionicons (via `@expo/vector-icons`) — conjunto de ícones integrado ao Expo
- **Web:** ícones SVG customizados quando necessário
