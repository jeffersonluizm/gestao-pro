import React from 'react';
import { StyleSheet, Text, TextProps, TextStyle } from 'react-native';
import { theme } from '../theme/theme';

// Texto do app com a família Inter: converte o fontWeight do estilo na variante carregada
// (no Android, fontWeight não escolhe o arquivo da fonte).
function familyFor(weight: TextStyle['fontWeight']) {
  const value = String(weight ?? '400');
  if (value === 'bold' || Number(value) >= 700) return theme.font.bold;
  if (Number(value) >= 600) return theme.font.semibold;
  if (Number(value) >= 500) return theme.font.medium;
  return theme.font.regular;
}

export default function AppText({ style, ...props }: TextProps) {
  const flat = StyleSheet.flatten(style) ?? {};
  return (
    <Text
      {...props}
      style={[{ color: theme.color.ink }, flat, { fontFamily: flat.fontFamily ?? familyFor(flat.fontWeight), fontWeight: undefined }]}
    />
  );
}
