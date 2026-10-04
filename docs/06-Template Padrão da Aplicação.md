# Template Padrão da Aplicação

Identidade visual e padrões de interface do Gestão Pro, iguais no painel web e no app. A fonte da verdade é o código: [`src/GestaoPro.Web/src/styles/tokens.css`](../src/GestaoPro.Web/src/styles/tokens.css) e [`src/GestaoPro.Mobile/src/theme/theme.ts`](../src/GestaoPro.Mobile/src/theme/theme.ts), com os **mesmos nomes** (`--gp-color-brand` no painel é `theme.color.brand` no app).

## Marca

- **Nome único:** Gestão Pro.
- **Logo:** monograma "GP" com prédios ([`gestao-pro-logo.svg`](../src/GestaoPro.Web/src/assets/gestao-pro-logo.svg)); favicon, ícone do app, ícone adaptativo do Android, splash e imagem de prévia de link (Open Graph) derivados dele.
- **Tom de voz:** português claro e direto, sem jargão técnico na tela; mensagens dizem o que aconteceu e o que fazer ("Sua sessão expirou. Entre novamente."). Sem emojis.

## Cores

A paleta nasce da logo: o **marinho** é a cor da marca, o **dourado** é o acento e o **teal** é a cor de ação (botões e links).

| Token | Valor | Uso | Contraste |
|---|---|---|---|
| `brand` | `#0E243F` | sidebar, hero, splash, títulos de destaque | branco sobre ele: 15,6:1 |
| `brandAccent` | `#C8953D` | ícones e detalhes **sobre o marinho**, item ativo do menu | sobre o marinho: 5,8:1; sobre branco só 2,7:1 (não usar em texto claro) |
| `accentText` | `#8A6220` | dourado para texto sobre fundo claro (rótulos "eyebrow") | 5,5:1 no branco |
| `accentSoft` | `#F7EDDC` | fundo de selos e destaques suaves | |
| `action` | `#176B87` | botões, links, foco | branco sobre ele: 6,0:1 |
| `actionHover` | `#0F5268` | hover e botão secundário | 8,7:1 |
| `ink` | `#17212B` | texto principal | 16,3:1 no branco |
| `inkMuted` | `#55626F` | texto secundário | 6,2:1 no branco; 5,8:1 no fundo |
| `background` | `#F5F7FB` | fundo das páginas | |
| `surface` | `#FFFFFF` | cartões e painéis | |
| `border` / `borderStrong` | `#DCE4EA` / `#8795A3` | divisórias / borda de campo (3,1:1) | |
| `success` / `danger` / `warning` | `#1F7A4D` / `#B42318` / `#8A5300` | estados, com fundos suaves `*Soft` | todos acima de 4,5:1 nos fundos suaves |

Contraste calculado pela fórmula da WCAG 2.1 (nível AA: 4,5:1 para texto, 3:1 para bordas de controle).

## Tipografia

- **Inter**, carregada de verdade: no painel por `@fontsource-variable/inter` (sem CDN), no app por `@expo-google-fonts/inter` (o componente `AppText` escolhe o peso).
- Escala do painel: 12, 14, 16, 18, 24, 32 px e título de destaque fluido (`clamp`).

## Espaçamento e forma

- Escala de 4 px (`--gp-space-1` a `--gp-space-8`: 4, 8, 12, 16, 24, 32, 48, 64).
- Raios: 6 px (controles), 10 px (cartões), 16 px (destaques); pílula para selos.
- Altura mínima de controle: 40 px no painel, 48 px nos botões principais do app.

## Ícones

Um só conjunto: **Ionicons** (`react-icons/io5` no painel, `@expo/vector-icons` no app), na versão contorno.

## Componentes

| Componente | Painel | App |
|---|---|---|
| Botões (primário, secundário, perigo, link) | `styles/components.css` | estilos por tela com `theme` |
| Campo com rótulo, dica e erro | `.field`, `PasswordInput` | `TextInput` com `accessibilityLabel` |
| Estados de lista (carregando, vazio, erro) | `components/ui/States.tsx` | `ActivityIndicator`, `ListEmptyComponent`, caixa de erro com "Tentar novamente" |
| Aviso de resultado | `Toast` (região `aria-live`) | `Alert` nativo |
| Confirmação de ação destrutiva | `ConfirmDialog` (`alertdialog`, foco no botão seguro, Esc cancela) | `Alert` com botões |
| Demonstração | `features/demo/DemoAccess.tsx` | `components/DemoAccessCard.tsx` |
| Servidor acordando | `ServerWakeNotice` | cartão no login |
| Marca | `Brand` | `assets/logo.png` |

## Layout

- **Painel:** páginas públicas (início, "Como foi feito"), telas de conexão em duas colunas (marca à esquerda, formulário à direita) e o painel com menu lateral por grupos (Comunicação, Portaria, Espaços, Financeiro, Cadastros), barra superior com a pessoa logada, Perfil e Sair. Abaixo de 800 px o menu vira gaveta com botão "Menu".
- **App:** onboarding no primeiro uso, login, abas (Início, Ocorrências, Reservas, Perfil, Mais) e a pilha do "Mais" com botão voltar.

## Acessibilidade

- Foco visível em tudo (`:focus-visible` com anel de 3 px), link "Pular para o conteúdo", `aria-current` no menu, `aria-live` em avisos e erros, tabelas com `caption` e `scope`.
- No app: `accessibilityRole` e `accessibilityLabel` em botões e campos, estado `expanded` nos cartões que abrem.
- Respeita `prefers-reduced-motion`.
