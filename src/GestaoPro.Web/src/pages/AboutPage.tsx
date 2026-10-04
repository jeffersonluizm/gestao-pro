import { Link } from 'react-router-dom'
import { ArchitectureDiagram } from '../components/ArchitectureDiagram'

const stack = ['.NET 10 / ASP.NET Core', 'MongoDB', 'JWT + BCrypt', 'SignalR', 'xUnit + Moq', 'React 18 + TypeScript', 'React Router', 'Vite + Vitest', 'React Native + Expo', 'k6 (carga)']

const decisoes = [
  ['Monólito modular em vez de microsserviços', 'Um time pequeno e um domínio só: uma API com camadas claras (controllers, serviços, repositórios) entrega o mesmo isolamento de regras sem o custo de rede, deploy e consistência distribuída. A proposta acadêmica de microsserviços fica registrada como evolução possível.'],
  ['Acesso decidido na API, pelo token', 'Cada leitura e escrita confere dono e condomínio a partir das claims do JWT (ResourceAccess). Item de outro condomínio responde 404, igual a inexistente, para não vazar existência.'],
  ['Filtro no banco, não em memória', 'As listas consultam só o condomínio do token, com índices e paginação opcional; antes liam a coleção inteira e filtravam na aplicação.'],
  ['Rotas anônimas sem pistas', 'Login, cadastro e primeiro acesso respondem mensagens únicas e rodam sempre uma verificação BCrypt, para o tempo de resposta não revelar se um e-mail ou CPF existe. Limite de tentativas por IP e por condomínio.'],
  ['Trava na reserva', 'A conferência de conflito de horário e a gravação acontecem sob uma trava por área e data: dois pedidos simultâneos não reservam o mesmo horário.'],
  ['Identidade visual a partir da logo', 'Tokens únicos (marinho e dourado da marca, teal como cor de ação) no painel e no app, com contraste AA conferido.'],
]

const limitacoes = [
  'Não há envio de e-mail: a recuperação de senha passa pela administração, que volta a conta para o código do condomínio.',
  'O token JWT continua válido até expirar mesmo depois de desativar a conta (não há lista de revogação).',
  'O token fica no localStorage do navegador; um cookie httpOnly reduziria o risco de XSS.',
  'Pagamentos são registro manual: não há integração com banco ou gateway.',
  'O teste de estresse roda numa única máquina (gerador, API e banco juntos): os números comparam antes e depois, não medem produção.',
]

// "Como foi feito": a página que um recrutador lê para entender arquitetura, decisões e limites.
export function AboutPage() {
  return (
    <>
      <section className="section" aria-labelledby="sobre-titulo">
        <header>
          <p className="eyebrow">Sobre o projeto</p>
          <h1 id="sobre-titulo">Como o Gestão Pro foi feito</h1>
          <p>
            Sistema de gestão condominial com API REST em .NET, painel web em React e app em React Native. Começou como projeto
            acadêmico (PUC Minas, Arquitetura de Sistemas Distribuídos, 2026/1) e foi revisado para portfólio: segurança, organização
            do código, identidade visual, documentação e teste de estresse.
          </p>
        </header>

        <div className="section-grid" style={{ marginTop: 0 }}>
          <article className="card prose">
            <h2>Para quem</h2>
            <p>Condomínios residenciais: moradores, portaria, síndico e administradora, cada um com a sua visão e as suas permissões.</p>
            <h2>Papel do Jefferson</h2>
            <p>
              No time acadêmico: Scrum Master e desenvolvimento full stack (autenticação, cadastro, avisos, reservas, áreas comuns,
              calendário e responsividade). Na revisão para portfólio: o modelo de acesso por dono e por condomínio, as correções de
              segurança, a reorganização do painel, as telas novas, a identidade visual e o teste de estresse.
            </p>
          </article>
          <article className="card">
            <h2>Tecnologias</h2>
            <ul className="stack-list" style={{ marginTop: 12 }}>
              {stack.map(item => <li key={item} className="badge">{item}</li>)}
            </ul>
          </article>
        </div>
      </section>

      <section className="section" aria-labelledby="arquitetura" style={{ paddingTop: 0 }}>
        <header>
          <p className="eyebrow">Arquitetura real</p>
          <h2 id="arquitetura">Dois clientes, uma API modular, um banco</h2>
        </header>
        <figure className="figure" style={{ margin: 0 }}>
          <ArchitectureDiagram />
          <figcaption>Painel e app consomem a mesma API. O acesso é decidido no servidor a partir do token; o banco guarda os dados de todos os condomínios, sempre consultados pelo condomínio de quem chama.</figcaption>
        </figure>
      </section>

      <section className="section" aria-labelledby="decisoes" style={{ paddingTop: 0 }}>
        <header>
          <p className="eyebrow">Decisões técnicas</p>
          <h2 id="decisoes">O que foi escolhido e por quê</h2>
        </header>
        <div className="feature-grid">
          {decisoes.map(([titulo, texto]) => (
            <article className="card feature" key={titulo}>
              <h3>{titulo}</h3>
              <p>{texto}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="section" aria-labelledby="qualidade" style={{ paddingTop: 0 }}>
        <div className="section-grid" style={{ marginTop: 0 }}>
          <article className="card prose" aria-labelledby="qualidade">
            <h2 id="qualidade">Testes e estresse</h2>
            <p>API com testes de unidade e de integração (xUnit, WebApplicationFactory) cobrindo acesso por papel, rotas anônimas, limites de tentativa e a corrida de reservas. Painel com testes de interface (Vitest + Testing Library) por papel.</p>
            <p>O teste de estresse (k6) compara a versão anterior com a atual num banco descartável: hipóteses escritas antes, medições, gráficos e conclusões ficam em <code>docs/carga/</code> no repositório.</p>
          </article>
          <article className="card prose">
            <h2>Limitações conhecidas</h2>
            <ul>{limitacoes.map(item => <li key={item}>{item}</li>)}</ul>
          </article>
        </div>
        <div className="hero-actions">
          <Link className="btn" to="/entrar">Entrar no painel</Link>
          <Link className="btn btn-secondary" to="/">Voltar ao início</Link>
        </div>
      </section>
    </>
  )
}
