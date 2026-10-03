import { Platform } from 'react-native';

// Web (navegador no mesmo PC) → localhost
// Nativo (celular na rede) → IP da máquina
export const API_BASE_URL = Platform.OS === 'web'
  ? 'http://localhost:5088'
  : 'http://192.168.18.112:5088';
