import logo from '../../assets/gestao-pro-logo.svg'

// Logo + nome único do produto ("Gestão Pro").
export function Brand({ tagline = 'Gestão de condomínio', compact = false }: { tagline?: string; compact?: boolean }) {
  return (
    <span className="brand">
      <img className="brand-logo" src={logo} alt="" width={40} height={40} />
      <span>
        <span className="brand-name">Gestão Pro</span>
        {!compact && <span className="brand-tagline">{tagline}</span>}
      </span>
    </span>
  )
}
