import { Dimensions, PixelRatio } from 'react-native';

const { width: W, height: H } = Dimensions.get('window');

// Dimensões base de design (Android médio / iPhone 14)
const BASE_W = 390;
const BASE_H = 844;

// Limita a largura efetiva ao intervalo de um celular real.
// Impede que sw() produza valores gigantes no web preview (onde W pode ser 1440px+).
const EFF_W = Math.min(Math.max(W, 320), 430);
const EFF_H = Math.min(Math.max(H, 600), 932);

/**
 * Escala horizontal proporcional à largura da tela.
 * Limitada ao intervalo 320–430dp para não distorcer no web.
 */
export const sw = (n: number): number =>
  Math.round(PixelRatio.roundToNearestPixel((EFF_W / BASE_W) * n));

/**
 * Escala vertical proporcional à altura da tela.
 * Prefira flex/ScrollView a sh() para containers.
 */
export const sh = (n: number): number =>
  Math.round(PixelRatio.roundToNearestPixel((EFF_H / BASE_H) * n));

/**
 * Escala de fonte — nunca menor que o valor base.
 */
export const sf = (n: number): number =>
  Math.round(PixelRatio.roundToNearestPixel(Math.max(n, (EFF_W / BASE_W) * n)));

/** Dimensões reais da tela (para uso em layout) */
export const SCREEN = { width: W, height: H };

/** Tela pequena (< 360dp) ou grande (> 430dp) */
export const isSmallScreen = EFF_W < 360;
export const isLargeScreen = EFF_W >= 430;

/** Padding horizontal padrão — ~5% da largura efetiva */
export const HP = Math.round(EFF_W * 0.053);
