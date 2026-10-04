import { Link } from 'react-router-dom'
import {
  IoCalendarOutline, IoCashOutline, IoCheckboxOutline, IoCubeOutline, IoMegaphoneOutline, IoPeopleOutline,
  IoPersonAddOutline, IoShieldCheckmarkOutline,
} from 'react-icons/io5'
import { useAuth } from '../auth/AuthContext'

const recursos = [
  { icon: IoMegaphoneOutline, titulo: 'Mural de avisos', texto: 'A administração publica; moradores e portaria leem no painel e no app.' },
  { icon: IoCalendarOutline, titulo: 'Reservas sem conflito', texto: 'Calendário das áreas comuns com checagem de horário e trava contra pedidos simultâneos.' },
  { icon: IoPersonAddOutline, titulo: 'Portaria', texto: 'Cadastro de visitantes com entrada e saída, e controle de encomendas até a retirada.' },
  { icon: IoCubeOutline, titulo: 'Encomendas', texto: 'O morador vê o que chegou para ele; a portaria registra chegada e retirada.' },
  { icon: IoCheckboxOutline, titulo: 'Assembleias e votos', texto: 'Pautas com votação por morador e apuração por pauta, um voto por pessoa.' },
  { icon: IoCashOutline, titulo: 'Financeiro', texto: 'Cobranças por morador e registro de pagamentos, cada um vendo só as suas.' },
]

const perfis = [
  { titulo: 'Morador', texto: 'Avisos, reservas, encomendas, cobranças, ocorrências e votações. Ativa o próprio acesso com o código do condomínio.' },
  { titulo: 'Portaria', texto: 'Visitantes do condomínio, entrada e saída, chegada e retirada de encomendas.' },
  { titulo: 'Síndico e administração', texto: 'Cadastros, aprovação de reservas, avisos, assembleias, financeiro e redefinição de senha. O síndico pode ver o sistema como morador.' },
]

// Página inicial pública: o que é o produto, para quem e por onde entrar.
export function LandingPage() {
  const { isLoggedIn } = useAuth()
  return (
    <>
      <section className="hero">
        <div className="hero-inner">
          <div>
            <p className="eyebrow" style={{ color: 'var(--gp-color-brand-accent)' }}>Gestão condominial</p>
            <h1>O dia a dia do condomínio, organizado num só lugar.</h1>
            <p>
              Gestão Pro junta avisos, reservas, portaria, encomendas, assembleias e financeiro num painel web e num app,
              com cada pessoa vendo só o que é dela.
            </p>
            <div className="hero-actions">
              <Link className="btn btn-large" to={isLoggedIn ? '/painel' : '/entrar'}>{isLoggedIn ? 'Ir para o painel' : 'Entrar no painel'}</Link>
              <Link className="btn btn-secondary btn-large" to="/sobre">Como foi feito</Link>
            </div>
          </div>
          <div className="hero-card" aria-label="Destaques técnicos">
            <ul className="list-plain" style={{ color: 'inherit' }}>
              <li style={{ background: 'transparent' }}><IoShieldCheckmarkOutline aria-hidden size={22} color="#c8953d" /> Acesso por dono e por condomínio (LGPD)</li>
              <li style={{ background: 'transparent' }}><IoPeopleOutline aria-hidden size={22} color="#c8953d" /> Quatro papéis: morador, portaria, síndico e administração</li>
              <li style={{ background: 'transparent' }}><IoCheckboxOutline aria-hidden size={22} color="#c8953d" /> Mais de 480 testes automatizados na API e no painel</li>
            </ul>
          </div>
        </div>
      </section>

      <section className="section" aria-labelledby="recursos">
        <header>
          <p className="eyebrow">Módulos</p>
          <h2 id="recursos">Tudo que um condomínio usa no mês</h2>
        </header>
        <div className="feature-grid">
          {recursos.map(({ icon: Icon, titulo, texto }) => (
            <article className="card feature" key={titulo}>
              <Icon aria-hidden size={26} />
              <h3>{titulo}</h3>
              <p>{texto}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="section" aria-labelledby="perfis" style={{ paddingTop: 0 }}>
        <header>
          <p className="eyebrow">Para quem</p>
          <h2 id="perfis">Cada perfil vê o que precisa</h2>
          <p>A API decide o acesso pelo token: dados de outro condomínio respondem como inexistentes, e o morador só enxerga os próprios registros.</p>
        </header>
        <div className="feature-grid">
          {perfis.map(perfil => (
            <article className="card feature" key={perfil.titulo}>
              <h3>{perfil.titulo}</h3>
              <p>{perfil.texto}</p>
            </article>
          ))}
        </div>
        <div className="hero-actions">
          <Link className="btn" to="/cadastro">Sou morador e quero ativar meu acesso</Link>
          <Link className="btn btn-secondary" to="/entrar">Entrar</Link>
        </div>
      </section>
    </>
  )
}
