// Identidade visual do Gestão Pro no app. Os nomes espelham os tokens do painel
// (src/GestaoPro.Web/src/styles/tokens.css): --gp-color-brand = theme.color.brand, e assim por diante.
// Origem: a logo (marinho #0E243F e dourado #C8953D); o teal #176B87 é a cor de ação.
export const theme = {
  color: {
    brand: '#0E243F',
    brandStrong: '#081A2F',
    brandAccent: '#C8953D', // só ícones e superfícies sobre o marinho (2,7:1 no branco)
    accentText: '#8A6220', // dourado escurecido para texto sobre claro (5,5:1)
    accentSoft: '#F7EDDC',
    onBrand: '#FFFFFF',
    onBrandMuted: '#B9C9D6',
    action: '#176B87', // botões e links (6,0:1 com branco)
    actionHover: '#0F5268',
    actionSoft: '#E9F6F9',
    onAction: '#FFFFFF',
    ink: '#17212B',
    inkMuted: '#55626F', // 5,8:1 sobre o fundo
    background: '#F5F7FB',
    surface: '#FFFFFF',
    surfaceSoft: '#EEF3F7',
    border: '#DCE4EA',
    borderStrong: '#8795A3', // borda de campo: 3:1 com o branco
    success: '#1F7A4D',
    successSoft: '#E9F7EE',
    danger: '#B42318',
    dangerSoft: '#FFF0ED',
    warning: '#8A5300',
    warningSoft: '#FFF6E5',
  },
  font: {
    regular: 'Inter_400Regular',
    medium: 'Inter_500Medium',
    semibold: 'Inter_600SemiBold',
    bold: 'Inter_700Bold',
  },
  space: { 1: 4, 2: 8, 3: 12, 4: 16, 5: 24, 6: 32, 7: 48, 8: 64 },
  radius: { sm: 6, md: 10, lg: 16, pill: 999 },
} as const;

export type ThemeColor = keyof typeof theme.color;
