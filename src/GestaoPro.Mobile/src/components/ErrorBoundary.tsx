import React from 'react';
import { ScrollView, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import AppText from './AppText';
import { theme } from '../theme/theme';

// Erro inesperado de renderização: mensagem amigável (o detalhe técnico vai só para o console).
export default class ErrorBoundary extends React.Component<{ children: React.ReactNode }, { failed: boolean }> {
  state = { failed: false };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  componentDidCatch(error: unknown) {
    console.error('Erro inesperado no app:', error);
  }

  render() {
    if (!this.state.failed) return this.props.children;
    return (
      <ScrollView contentContainerStyle={{ flexGrow: 1, justifyContent: 'center', padding: 32, backgroundColor: theme.color.background }}>
        <View style={{ alignItems: 'center', gap: 12 }} accessibilityRole="alert">
          <Ionicons name="alert-circle-outline" size={56} color={theme.color.danger} />
          <AppText style={{ fontSize: 20, fontWeight: '700', textAlign: 'center' }}>Algo deu errado</AppText>
          <AppText style={{ color: theme.color.inkMuted, textAlign: 'center' }}>
            A tela não pôde ser exibida. Tente de novo; se continuar, feche e abra o app.
          </AppText>
          <TouchableOpacity
            accessibilityRole="button"
            onPress={() => this.setState({ failed: false })}
            style={{ marginTop: 8, backgroundColor: theme.color.action, borderRadius: 10, paddingHorizontal: 24, paddingVertical: 12 }}
          >
            <AppText style={{ color: theme.color.onAction, fontWeight: '700' }}>Tentar de novo</AppText>
          </TouchableOpacity>
        </View>
      </ScrollView>
    );
  }
}
