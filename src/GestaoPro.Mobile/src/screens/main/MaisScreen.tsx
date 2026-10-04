import React from 'react';
import {
  View, TouchableOpacity, StyleSheet, ScrollView,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { MaisStackParamList } from '../../navigation/AppNavigator';
import { useAuth } from '../../context/AuthContext';
import { sw, sf, HP } from '../../utils/responsive';

import { theme } from '../../theme/theme';
import AppText from '../../components/AppText';
type Nav = NativeStackNavigationProp<MaisStackParamList>;

type Audience = 'gestao' | 'morador' | 'porteiro';

// Atalhos do "Mais", por grupo e por papel (a API decide o acesso; o app só não oferece o que vai ser recusado).
const ITEMS: {
  name: Exclude<keyof MaisStackParamList, 'MaisInicio'>;
  label: string;
  icon: string;
  desc: string;
  audience: Audience[];
}[] = [
  { name: 'Encomendas', label: 'Encomendas', icon: 'cube-outline', desc: 'Chegadas e retiradas na portaria', audience: ['gestao', 'morador', 'porteiro'] },
  { name: 'Visitantes', label: 'Visitantes', icon: 'person-add-outline', desc: 'Cadastro, entrada e saída', audience: ['gestao', 'morador', 'porteiro'] },
  { name: 'Financeiro', label: 'Financeiro', icon: 'cash-outline', desc: 'Cobranças e pagamentos', audience: ['gestao', 'morador'] },
  { name: 'Assembleias', label: 'Assembleias', icon: 'people-outline', desc: 'Reuniões e pautas', audience: ['gestao', 'morador', 'porteiro'] },
  { name: 'Votos', label: 'Votos', icon: 'checkbox-outline', desc: 'Votações abertas', audience: ['gestao', 'morador', 'porteiro'] },
  { name: 'Áreas Comuns', label: 'Áreas comuns', icon: 'key-outline', desc: 'Espaços reserváveis', audience: ['gestao', 'morador', 'porteiro'] },
  { name: 'Pets', label: 'Pets', icon: 'paw-outline', desc: 'Animais da unidade', audience: ['gestao', 'morador'] },
  { name: 'Condomínios', label: 'Condomínio', icon: 'business-outline', desc: 'Dados do condomínio', audience: ['gestao'] },
  { name: 'Unidades', label: 'Unidades', icon: 'home-outline', desc: 'Apartamentos e casas', audience: ['gestao'] },
  { name: 'Usuários', label: 'Usuários', icon: 'id-card-outline', desc: 'Moradores, portaria e síndicos', audience: ['gestao'] },
];

function audienceOf(role?: string): Audience {
  const value = (role ?? '').toLowerCase();
  if (value === 'admin' || value === 'sindico') return 'gestao';
  return value === 'porteiro' ? 'porteiro' : 'morador';
}

export default function MaisScreen() {
  const navigation = useNavigation<Nav>();
  const { session } = useAuth();

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <AppText style={styles.title}>Mais</AppText>
        <AppText style={styles.subtitle}>Olá, {session?.userName?.split(' ')[0] ?? 'Usuário'}</AppText>
      </View>

      <ScrollView contentContainerStyle={styles.grid}>
        {ITEMS.filter(item => item.audience.includes(audienceOf(session?.role))).map(item => (
          <TouchableOpacity accessibilityRole="button"
            accessibilityLabel={`${item.label}: ${item.desc}`}
            key={item.name}
            style={styles.card}
            onPress={() => navigation.navigate(item.name)}
            activeOpacity={0.75}
          >
            <View style={styles.iconWrap}>
              <Ionicons name={item.icon as never} size={sw(28)} color={theme.color.action} />
            </View>
            <View style={styles.cardText}>
              <AppText style={styles.cardLabel}>{item.label}</AppText>
              <AppText style={styles.cardDesc}>{item.desc}</AppText>
            </View>
            <Ionicons name="chevron-forward" size={sw(18)} color={theme.color.borderStrong} />
          </TouchableOpacity>
        ))}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.color.background },
  header: {
    paddingHorizontal: HP, paddingVertical: sw(16),
    backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: theme.color.border,
  },
  title: { fontSize: sf(20), fontWeight: '700', color: theme.color.ink },
  subtitle: { fontSize: sf(13), color: theme.color.inkMuted, marginTop: sw(2) },
  grid: { padding: sw(16), gap: sw(10) },
  card: {
    backgroundColor: '#fff', borderRadius: sw(14), padding: sw(16),
    flexDirection: 'row', alignItems: 'center', gap: sw(14),
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06, shadowRadius: 4, elevation: 2,
  },
  iconWrap: {
    width: sw(52), height: sw(52), borderRadius: sw(14), backgroundColor: theme.color.actionSoft,
    justifyContent: 'center', alignItems: 'center',
  },
  cardText: { flex: 1 },
  cardLabel: { fontSize: sf(16), fontWeight: '700', color: theme.color.ink },
  cardDesc:  { fontSize: sf(13), color: theme.color.inkMuted, marginTop: sw(2) },
});
