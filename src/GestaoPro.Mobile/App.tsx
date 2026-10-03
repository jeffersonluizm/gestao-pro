// IMPORTANTE: este import deve ser a primeira linha do arquivo
import 'react-native-gesture-handler';

import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { View, Text, ScrollView, Platform, ActivityIndicator } from 'react-native';
import { useFonts } from 'expo-font';
import { AuthProvider } from './src/context/AuthContext';
import AppNavigator from './src/navigation/AppNavigator';

// Garante altura 100% no web
if (Platform.OS === 'web') {
  const style = document.createElement('style');
  style.textContent = 'html, body, #root { height: 100%; margin: 0; padding: 0; }';
  document.head.appendChild(style);
}

class ErrorBoundary extends React.Component<
  { children: React.ReactNode },
  { error: Error | null }
> {
  state = { error: null };
  static getDerivedStateFromError(error: Error) {
    return { error };
  }
  render() {
    if (this.state.error) {
      return (
        <ScrollView style={{ flex: 1, backgroundColor: '#fff', padding: 24, paddingTop: 60 }}>
          <Text style={{ fontSize: 18, fontWeight: 'bold', color: '#dc2626', marginBottom: 12 }}>
            Erro ao carregar o app:
          </Text>
          <Text style={{ fontSize: 13, color: '#334155', fontFamily: 'monospace' }}>
            {String(this.state.error)}
          </Text>
        </ScrollView>
      );
    }
    return this.props.children;
  }
}

export default function App() {
  const [fontsLoaded] = useFonts({
    Ionicons: require('@expo/vector-icons/build/vendor/react-native-vector-icons/Fonts/Ionicons.ttf'),
    MaterialIcons: require('@expo/vector-icons/build/vendor/react-native-vector-icons/Fonts/MaterialIcons.ttf'),
  });

  if (!fontsLoaded) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: '#f8fafc' }}>
        <ActivityIndicator size="large" color="#176b87" />
      </View>
    );
  }

  return (
    <ErrorBoundary>
      <GestureHandlerRootView style={{ flex: 1 }}>
        <AuthProvider>
          <StatusBar style="dark" />
          <AppNavigator />
        </AuthProvider>
      </GestureHandlerRootView>
    </ErrorBoundary>
  );
}
