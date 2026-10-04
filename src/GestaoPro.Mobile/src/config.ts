import { Platform } from 'react-native';

// Endereço da API. No celular, "localhost" é o próprio aparelho: defina
// EXPO_PUBLIC_API_URL com o IP do PC na rede (ex.: http://192.168.0.10:5088),
// no arquivo .env (copie de .env.example) ou deixe o iniciar.bat detectar.
// Sem a variável: navegador no mesmo PC usa localhost; nativo também, com aviso.
const DEFAULT_API_URL = 'http://localhost:5088';
const configuredApiUrl = process.env.EXPO_PUBLIC_API_URL?.trim().replace(/\/+$/, '');

if (!configuredApiUrl && Platform.OS !== 'web') {
  console.warn(
    'EXPO_PUBLIC_API_URL não definida: o app vai procurar a API em localhost, ' +
      'que no celular não é o seu PC. Veja src/GestaoPro.Mobile/.env.example.'
  );
}

export const API_BASE_URL = configuredApiUrl || DEFAULT_API_URL;
