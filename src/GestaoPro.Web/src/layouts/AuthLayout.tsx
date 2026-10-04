import { Link, Outlet } from 'react-router-dom'
import { IoCalendarOutline, IoCubeOutline, IoMegaphoneOutline, IoShieldCheckmarkOutline } from 'react-icons/io5'
import { Brand } from '../components/ui/Brand'

// Telas de conexão (entrar, cadastro, primeiro acesso, recuperar senha): marca à esquerda, formulário à direita.
export function AuthLayout() {
  return (
    <div className="auth-layout">
      <aside className="auth-aside">
        <Link to="/" className="brand" aria-label="Gestão Pro, página inicial">
          <Brand tagline="Eficiência para o seu condomínio" />
        </Link>
        <div className="form-stack">
          <h2>O condomínio inteiro num lugar só.</h2>
          <p>Avisos, reservas, portaria, encomendas, assembleias e financeiro, com cada pessoa vendo só o que é dela.</p>
          <ul>
            <li><IoMegaphoneOutline aria-hidden size={20} /> Avisos da administração no mural</li>
            <li><IoCalendarOutline aria-hidden size={20} /> Reserva de áreas comuns sem conflito de horário</li>
            <li><IoCubeOutline aria-hidden size={20} /> Encomendas e visitantes registrados pela portaria</li>
            <li><IoShieldCheckmarkOutline aria-hidden size={20} /> Dados separados por condomínio e por morador</li>
          </ul>
        </div>
        <p className="auth-aside-extra"><Link to="/sobre" style={{ color: 'inherit' }}>Como o projeto foi feito</Link></p>
      </aside>
      <main className="auth-main" id="conteudo">
        <Outlet />
      </main>
    </div>
  )
}
