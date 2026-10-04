import React from 'react';
import { TouchableOpacity } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useNavigation } from '@react-navigation/native';
import { theme } from '../theme/theme';

// Voltar das telas abertas pelo "Mais" (pilha de navegação). Some quando não há para onde voltar.
export default function BackButton() {
  const navigation = useNavigation();
  if (!navigation.canGoBack()) return null;
  return (
    <TouchableOpacity
      onPress={() => navigation.goBack()}
      accessibilityRole="button"
      accessibilityLabel="Voltar"
      hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
      style={{ marginRight: 8 }}
    >
      <Ionicons name="arrow-back" size={24} color={theme.color.ink} />
    </TouchableOpacity>
  );
}
