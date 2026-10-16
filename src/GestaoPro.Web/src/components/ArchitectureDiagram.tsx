// Arquitetura real: dois clientes, uma API monolítica modular e um MongoDB.
export function ArchitectureDiagram() {
  const box = { rx: 12, strokeWidth: 1.5 }
  return (
    <svg viewBox="0 0 760 360" role="img" aria-labelledby="arq-titulo arq-desc" style={{ width: '100%', height: 'auto' }}>
      <title id="arq-titulo">Arquitetura do Gestão Pro</title>
      <desc id="arq-desc">
        Painel web em React e app em Expo chamam a API REST ASP.NET Core por HTTPS com token JWT. A API tem controllers,
        middleware de acesso, serviços e repositórios, e grava num único banco MongoDB. Notificações em tempo real via SignalR.
      </desc>
      <defs>
        <marker id="seta" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
          <path d="M0,0 L10,5 L0,10 z" fill="#55626f" />
        </marker>
      </defs>
      <rect x="20" y="40" width="190" height="80" fill="#e9f6f9" stroke="#176b87" {...box} />
      <text x="115" y="74" textAnchor="middle" fontWeight="700" fill="#0e243f">Painel web</text>
      <text x="115" y="96" textAnchor="middle" fontSize="13" fill="#55626f">React + Vite + Router</text>
      <rect x="20" y="220" width="190" height="80" fill="#e9f6f9" stroke="#176b87" {...box} />
      <text x="115" y="254" textAnchor="middle" fontWeight="700" fill="#0e243f">App do morador</text>
      <text x="115" y="276" textAnchor="middle" fontSize="13" fill="#55626f">React Native + Expo</text>

      <rect x="290" y="20" width="250" height="300" fill="#0e243f" stroke="#0e243f" {...box} />
      <text x="415" y="50" textAnchor="middle" fontWeight="700" fill="#ffffff">API REST (.NET 10)</text>
      {['Middleware de acesso + rate limit', 'Controllers (decisão por dono e condomínio)', 'Serviços (regras de negócio)', 'Repositórios (MongoDB.Driver)'].map((label, index) => (
        <g key={label}>
          <rect x="305" y={70 + index * 58} width="220" height="44" rx="8" fill="#163659" stroke="#c8953d" strokeWidth="1" />
          <text x="415" y={97 + index * 58} textAnchor="middle" fontSize="12.5" fill="#ffffff">{label}</text>
        </g>
      ))}

      <rect x="610" y="130" width="130" height="90" fill="#f7eddc" stroke="#c8953d" {...box} />
      <text x="675" y="168" textAnchor="middle" fontWeight="700" fill="#0e243f">MongoDB</text>
      <text x="675" y="190" textAnchor="middle" fontSize="12.5" fill="#55626f">um banco, índices</text>

      <line x1="210" y1="80" x2="288" y2="120" stroke="#55626f" strokeWidth="1.6" markerEnd="url(#seta)" />
      <line x1="210" y1="260" x2="288" y2="220" stroke="#55626f" strokeWidth="1.6" markerEnd="url(#seta)" />
      <text x="248" y="176" textAnchor="middle" fontSize="12" fill="#55626f">HTTPS + JWT</text>
      <line x1="540" y1="175" x2="608" y2="175" stroke="#55626f" strokeWidth="1.6" markerEnd="url(#seta)" />
      <text x="415" y="345" textAnchor="middle" fontSize="12" fill="#55626f">SignalR (/notificacaoHub) para avisos em tempo real</text>
    </svg>
  )
}
