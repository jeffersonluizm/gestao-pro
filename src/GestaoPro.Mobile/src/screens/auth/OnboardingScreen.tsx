import React, { useState } from 'react';
import { Image, StyleSheet, TouchableOpacity, View } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';

import AppText from '../../components/AppText';
import { AuthStackParamList } from '../../navigation/AppNavigator';
import { theme } from '../../theme/theme';

export const ONBOARDING_KEY = 'gestaopro_onboarding_visto';

const SLIDES = [
  { icon: 'megaphone-outline', title: 'Avisos do condomínio', text: 'Tudo que a administração publica chega no mural do app, mais recentes primeiro.' },
  { icon: 'calendar-outline', title: 'Reservas sem conflito', text: 'Escolha a área, o dia e o horário. O sistema não deixa dois moradores reservarem o mesmo horário.' },
  { icon: 'cube-outline', title: 'Encomendas e visitas', text: 'A portaria registra o que chegou para você e quem veio visitar. Você acompanha pelo app.' },
] as const;

type Props = { navigation: NativeStackNavigationProp<AuthStackParamList, 'Onboarding'> };

// Apresentação no primeiro uso (3 telas). Depois de vista, o app abre direto no login.
export default function OnboardingScreen({ navigation }: Props) {
  const [index, setIndex] = useState(0);
  const slide = SLIDES[index];
  const last = index === SLIDES.length - 1;

  async function finish() {
    await AsyncStorage.setItem(ONBOARDING_KEY, '1').catch(() => undefined);
    navigation.replace('Login');
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.top}>
        <Image source={require('../../../assets/logo.png')} style={styles.logo} accessibilityIgnoresInvertColors accessible accessibilityLabel="Gestão Pro" />
        <TouchableOpacity accessibilityRole="button" onPress={finish} hitSlop={12}>
          <AppText style={styles.skip}>Pular</AppText>
        </TouchableOpacity>
      </View>

      <View style={styles.body}>
        <View style={styles.iconCircle}>
          <Ionicons name={slide.icon as never} size={56} color={theme.color.brandAccent} />
        </View>
        <AppText style={styles.title} accessibilityRole="header">{slide.title}</AppText>
        <AppText style={styles.text}>{slide.text}</AppText>
      </View>

      <View style={styles.footer}>
        <View style={styles.dots} accessible accessibilityLabel={`Tela ${index + 1} de ${SLIDES.length}`}>
          {SLIDES.map((item, i) => <View key={item.title} style={[styles.dot, i === index && styles.dotActive]} />)}
        </View>
        <TouchableOpacity accessibilityRole="button" style={styles.button} onPress={() => (last ? finish() : setIndex(i => i + 1))}>
          <AppText style={styles.buttonText}>{last ? 'Começar' : 'Próximo'}</AppText>
        </TouchableOpacity>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.color.brand, paddingHorizontal: 24 },
  top: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingTop: 12 },
  logo: { width: 48, height: 48, borderRadius: 10 },
  skip: { color: theme.color.onBrandMuted, fontSize: 15, fontWeight: '600' },
  body: { flex: 1, justifyContent: 'center', alignItems: 'center', gap: 16 },
  iconCircle: { width: 120, height: 120, borderRadius: 60, backgroundColor: 'rgba(200,149,61,0.15)', alignItems: 'center', justifyContent: 'center', marginBottom: 12 },
  title: { color: theme.color.onBrand, fontSize: 26, fontWeight: '700', textAlign: 'center' },
  text: { color: theme.color.onBrandMuted, fontSize: 16, textAlign: 'center', lineHeight: 24 },
  footer: { paddingBottom: 24, gap: 20 },
  dots: { flexDirection: 'row', justifyContent: 'center', gap: 8 },
  dot: { width: 8, height: 8, borderRadius: 4, backgroundColor: 'rgba(255,255,255,0.3)' },
  dotActive: { width: 24, backgroundColor: theme.color.brandAccent },
  button: { backgroundColor: theme.color.action, borderRadius: 12, paddingVertical: 16, alignItems: 'center' },
  buttonText: { color: theme.color.onAction, fontSize: 16, fontWeight: '700' },
});
