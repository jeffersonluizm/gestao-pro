import { Link } from 'react-router-dom'

// Recuperação honesta: o sistema não envia e-mail (não há serviço de e-mail configurado), então a página
// explica o caminho que existe de verdade: a administração redefine e a pessoa cria uma senha nova.
export function RecoverPasswordPage() {
  return (
    <div className="auth-card">
      <header>
        <p className="eyebrow">Acesso</p>
        <h1>Esqueci minha senha</h1>
        <p>O Gestão Pro não envia links por e-mail. A troca é feita com a administração do condomínio, em três passos:</p>
      </header>
      <ol className="prose" style={{ paddingLeft: 20 }}>
        <li>Peça à administração do condomínio (síndico ou administradora) para <strong>redefinir a sua senha</strong> no painel, em Usuários.</li>
        <li>O sistema gera uma <strong>senha temporária</strong>, que a administração entrega a você.</li>
        <li>Entre com o seu e-mail e a senha temporária: o sistema pede uma <strong>senha nova</strong> antes de liberar o acesso.</li>
      </ol>
      <div className="alert alert-info">
        Por segurança, ninguém da administração vê a sua senha: ela só consegue gerar uma senha temporária, que você troca no primeiro acesso.
      </div>
      <div className="form-actions" style={{ justifyContent: 'flex-start' }}>
        <Link className="btn" to="/entrar">Voltar para Entrar</Link>
      </div>
    </div>
  )
}
