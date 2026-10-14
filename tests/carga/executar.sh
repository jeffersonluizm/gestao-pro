#!/usr/bin/env bash
# Executa a bateria H1-H8 contra um build publicado, num banco descartável.
#   bash executar.sh <antes|depois> <porta> <pasta do publish> <pasta de saída>
# Cria a trava .carga-lock (uma carga por vez na máquina) e sempre a remove no fim.
set -euo pipefail

VARIANTE="$1"; PORTA="$2"; API_DIR="$3"; SAIDA="$4"
AQUI="$(cd "$(dirname "$0")" && pwd)"
TRAVA="${TRAVA:-$AQUI/../../../../.carga-lock}"
K6="${K6:-/c/Program Files/k6/k6.exe}"
API="http://127.0.0.1:$PORTA"
mkdir -p "$SAIDA"

mkdir "$TRAVA" || { echo "[carga] outra carga em andamento (trava existe): $TRAVA"; exit 2; }
parar() {
  powershell -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$AQUI/parar-ambiente.ps1")" -Trabalho "$(cygpath -w "$SAIDA")" || true
  rmdir "$TRAVA" 2>/dev/null || true
}
trap parar EXIT

powershell -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$AQUI/subir-ambiente.ps1")" \
  -Api "$(cygpath -w "$API_DIR")" -Porta "$PORTA" -Trabalho "$(cygpath -w "$SAIDA")"
API_PID="$(cut -d' ' -f2 "$SAIDA/pids.txt")"

for i in $(seq 1 60); do
  curl -s -o /dev/null -w '%{http_code}' "$API/api/health/mongodb" | grep -q 200 && break
  sleep 1
done

API="$API" node "$AQUI/portao.mjs"
API="$API" SAIDA="$SAIDA/estado.json" node "$AQUI/semear.mjs" base

lerEstado() { node -p "require(process.argv[1])['$1']" "$(cygpath -w "$SAIDA/estado.json")"; }
TOKEN="$(lerEstado token)"; CONDOMINIO="$(lerEstado condominiumId)"; USUARIO="$(lerEstado userId)"
AREA="$(lerEstado commonAreaId)"; ASSEMBLEIA="$(lerEstado assemblyId)"
EMAIL="$(lerEstado email)"; SENHA="$(lerEstado senha)"

amostra() { powershell -NoProfile -Command "\$p=Get-Process -Id $API_PID; '{0:F2} {1:F0}' -f \$p.TotalProcessorTime.TotalSeconds, (\$p.WorkingSet64/1MB)"; }

rodar() { # nome script [args k6...]
  local nome="$1" script="$2"; shift 2
  local antes; antes="$(amostra)"
  # MSYS_NO_PATHCONV: sem ele o Git Bash troca "-e CAMINHO=/api/..." por um caminho do Windows.
  MSYS_NO_PATHCONV=1 "$K6" run --quiet --summary-export "$(cygpath -w "$SAIDA/$nome.json")" -e API="$API" "$@" "$(cygpath -w "$AQUI/k6/$script")" > "$SAIDA/$nome.txt" 2>&1 || true
  echo "$nome $antes $(amostra)" >> "$SAIDA/recursos.txt"
  echo "[carga] $nome ok"
}

if [ "$VARIANTE" = "antes" ]; then LISTA_PUBLICA=/api/Condominiums; else LISTA_PUBLICA=/api/Condominiums/public; fi

rodar aquecimento get.js -e TOKEN="$TOKEN" -e CAMINHO=/api/announcements -e VUS=10 -e DURACAO=10s
for v in 10 50 100 200; do rodar "h2-avisos-$v" get.js -e TOKEN="$TOKEN" -e CAMINHO=/api/announcements -e VUS=$v -e DURACAO=30s; done
for v in 10 20 50; do rodar "h4-lista-publica-$v" get.js -e ANONIMO=1 -e CAMINHO=$LISTA_PUBLICA -e VUS=$v -e DURACAO=30s; done
rodar h8-health-50 get.js -e ANONIMO=1 -e CAMINHO=/api/health/mongodb -e VUS=50 -e DURACAO=30s
for v in 10 50; do rodar "h1-login-$v" login.js -e EMAIL="$EMAIL" -e SENHA="$SENHA" -e VUS=$v -e DURACAO=30s; done
rodar h1b-tempo-login tempo-login.js -e EMAIL="$EMAIL" -e SENHA="$SENHA" -e ITERACOES=200

DATA="$(date -d '+3 day' +%Y-%m-%d)"
rodar h6-reserva simultaneos.js -e TIPO=reserva -e N=50 -e TOKEN="$TOKEN" -e AREA="$AREA" -e USUARIO="$USUARIO" -e CONDOMINIO="$CONDOMINIO" -e DATA="$DATA"
curl -s -H "Authorization: Bearer $TOKEN" "$API/api/reservations" \
  | node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const r=JSON.parse(s).filter(x=>x.date==='$DATA'&&x.commonAreaId==='$AREA');console.log('reservas_gravadas',r.length)})" >> "$SAIDA/contagens.txt"

rodar h7-voto simultaneos.js -e TIPO=voto -e N=50 -e TOKEN="$TOKEN" -e ASSEMBLEIA="$ASSEMBLEIA"
curl -s -H "Authorization: Bearer $TOKEN" "$API/api/vote/assembly/$ASSEMBLEIA" \
  | node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const r=JSON.parse(s).filter(x=>!x.summaryOnly);console.log('votos_gravados',r.length)})" >> "$SAIDA/contagens.txt"

API="$API" node "$AQUI/semear.mjs" ruido 20000
for v in 10 50 100; do rodar "h3-avisos-com-ruido-$v" get.js -e TOKEN="$TOKEN" -e CAMINHO=/api/announcements -e VUS=$v -e DURACAO=30s; done

# H5 por último entre as medições de avisos: cada escrita cria um aviso no condomínio lido em H2 e H3.
rodar h5-escrita-50 escrita.js -e TOKEN="$TOKEN" -e CONDOMINIO="$CONDOMINIO" -e USUARIO="$USUARIO" -e VUS=50 -e DURACAO=30s

if [ "${SOAK:-0}" = "1" ]; then
  : > "$SAIDA/h9-memoria.txt"
  ( for i in $(seq 1 21); do echo "$(date +%s) $(amostra)" >> "$SAIDA/h9-memoria.txt"; sleep 30; done ) &
  rodar h9-soak get.js -e TOKEN="$TOKEN" -e CAMINHO="${SOAK_CAMINHO:-/api/announcements?page=1&pageSize=20}" -e VUS="${SOAK_VUS:-25}" -e DURACAO=10m
  wait
fi

echo "[carga] $VARIANTE concluída em $SAIDA"
