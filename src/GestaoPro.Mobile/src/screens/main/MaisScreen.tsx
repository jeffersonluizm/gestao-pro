import React from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet, ScrollView,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import { BottomTabNavigationProp } from '@react-navigation/bottom-tabs';
import { TabParamList } from '../../navigation/AppNavigator';
import { useAuth } from '../../context/AuthContext';
import { sw, sf, HP } from '../../utils/responsive';

type Nav = BottomTabNavigationProp<TabParamList>;

const ITEMS: {
  name: keyof TabParamList;
  label: string;
  icon: string;
  color: string;
  desc: string;
  adminOnly?: boolean;
}[] = [
  { name: 'Assembleias', label: 'Assembleias',  icon: 'people',   color: '#176b87', desc: 'Reuniões e pautas' },
  { name: 'Votos',       label: 'Votos',         icon: 'checkbox', color: '#10b981', desc: 'Votações ativas' },
  { name: 'Entregas',    label: 'Entregas',       icon: 'cube',     color: '#f59e0b', desc: 'Controle de encomendas' },
  { name: 'Encomendas',  label: 'Encomendas',     icon: 'gift',     color: '#8b5cf6', desc: 'Pacotes e retiradas' },
  { name: 'Pets',        label: 'Pets',           icon: 'paw',      color: '#ef4444', desc: 'Animais do condomínio' },
  { name: 'Condomínios', label: 'Condomínios',    icon: 'business', color: '#0f766e', desc: 'Cadastro e consulta', adminOnly: true },
  { name: 'Usuários',    label: 'Usuários',       icon: 'people',   color: '#2563eb', desc: 'Cadastro e gestão', adminOnly: true },
  { name: 'Unidades',    label: 'Unidades',       icon: 'home',     color: '#7c3aed', desc: 'Apartamentos e casas', adminOnly: true },
  { name: 'Áreas Comuns', label: 'Áreas Comuns',  icon: 'calendar', color: '#0891b2', desc: 'Espaços reserváveis' },
  { name: 'Visitantes',   label: 'Visitantes',     icon: 'person-add', color: '#ea580c', desc: 'Cadastro, entrada e saída' },
];

export default function MaisScreen() {
  const navigation = useNavigation<Nav>();
  const { session } = useAuth();

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Mais</Text>
        <Text style={styles.subtitle}>Olá, {session?.userName?.split(' ')[0] ?? 'Usuário'}</Text>
      </View>

      <ScrollView contentContainerStyle={styles.grid}>
        {ITEMS.filter(item => !item.adminOnly || ['admin', 'sindico'].includes((session?.role ?? '').toLowerCase())).map(item => (
          <TouchableOpacity
            key={item.name}
            style={styles.card}
            onPress={() => navigation.navigate(item.name)}
            activeOpacity={0.75}
          >
            <View style={[styles.iconWrap, { backgroundColor: item.color + '18' }]}>
              <Ionicons name={item.icon as any} size={sw(30)} color={item.color} />
            </View>
            <View style={styles.cardText}>
              <Text style={styles.cardLabel}>{item.label}</Text>
              <Text style={styles.cardDesc}>{item.desc}</Text>
            </View>
            <Ionicons name="chevron-forward" size={sw(18)} color="#cbd5e1" />
          </TouchableOpacity>
        ))}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  header: {
    paddingHorizontal: HP, paddingVertical: sw(16),
    backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0',
  },
  title: { fontSize: sf(20), fontWeight: '700', color: '#1e293b' },
  subtitle: { fontSize: sf(13), color: '#64748b', marginTop: sw(2) },
  grid: { padding: sw(16), gap: sw(10) },
  card: {
    backgroundColor: '#fff', borderRadius: sw(14), padding: sw(16),
    flexDirection: 'row', alignItems: 'center', gap: sw(14),
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06, shadowRadius: 4, elevation: 2,
  },
  iconWrap: {
    width: sw(52), height: sw(52), borderRadius: sw(14),
    justifyContent: 'center', alignItems: 'center',
  },
  cardText: { flex: 1 },
  cardLabel: { fontSize: sf(16), fontWeight: '700', color: '#1e293b' },
  cardDesc:  { fontSize: sf(13), color: '#64748b', marginTop: sw(2) },
});
