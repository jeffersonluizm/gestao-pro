import React, { useCallback, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, RefreshControl,
  TouchableOpacity, ActivityIndicator,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';

import { getAnnouncements, Announcement, getFriendlyErrorMessage } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { sw, sf, HP } from '../../utils/responsive';

const categoryColors: Record<string, string> = {
  Manutenção: '#f59e0b',
  Segurança: '#ef4444',
  Evento: '#8b5cf6',
  Financeiro: '#10b981',
  Geral: '#3b82f6',
};

function AnnouncementCard({ item }: { item: Announcement }) {
  const [expanded, setExpanded] = useState(false);
  const color = categoryColors[item.category] ?? '#3b82f6';
  const date = item.createdAt ? new Date(item.createdAt).toLocaleDateString('pt-BR') : '';

  return (
    <TouchableOpacity style={styles.card} onPress={() => setExpanded(v => !v)} activeOpacity={0.85}>
      <View style={styles.cardHeader}>
        <View style={[styles.categoryBadge, { backgroundColor: color + '20' }]}>
          <Text style={[styles.categoryText, { color }]}>{item.category ?? 'Geral'}</Text>
        </View>
        <Text style={styles.dateText}>{date}</Text>
      </View>
      <Text style={styles.cardTitle}>{item.title}</Text>
      {expanded && <Text style={styles.cardContent}>{item.content}</Text>}
      <Text style={styles.expandHint}>{expanded ? 'Ver menos ▲' : 'Ver mais ▼'}</Text>
    </TouchableOpacity>
  );
}

export default function HomeScreen() {
  const { session } = useAuth();
  const [announcements, setAnnouncements] = useState<Announcement[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');

  async function load(silent = false) {
    if (!silent) setLoading(true);
    setError('');
    try {
      const data = await getAnnouncements();
      setAnnouncements(data.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()));
    } catch (e: any) {
      console.error('Erro ao carregar avisos:', e);
      setError(getFriendlyErrorMessage(e, 'Não foi possível carregar os avisos.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }

  useFocusEffect(useCallback(() => { load(); }, []));

  return (
    <SafeAreaView style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <View>
          <Text style={styles.greeting}>Olá, {session?.userName?.split(' ')[0] ?? 'Morador'} 👋</Text>
          <Text style={styles.headerSub}>Avisos do condomínio</Text>
        </View>
        <View style={styles.iconCircle}>
          <Ionicons name="notifications-outline" size={sw(22)} color="#176b87" />
        </View>
      </View>

      {loading ? (
        <ActivityIndicator size="large" color="#176b87" style={{ marginTop: sw(60) }} />
      ) : error ? (
        <View style={styles.errorBox}>
          <Ionicons name="alert-circle-outline" size={sw(32)} color="#ef4444" />
          <Text style={styles.errorText}>{error}</Text>
          <TouchableOpacity style={styles.retryBtn} onPress={() => load()}>
            <Text style={styles.retryText}>Tentar novamente</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={announcements}
          keyExtractor={item => item.id}
          renderItem={({ item }) => <AnnouncementCard item={item} />}
          contentContainerStyle={styles.list}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(true); }} colors={['#176b87']} />}
          ListEmptyComponent={
            <View style={styles.empty}>
              <Ionicons name="megaphone-outline" size={sw(48)} color="#cbd5e1" />
              <Text style={styles.emptyText}>Nenhum aviso no momento.</Text>
            </View>
          }
        />
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  header: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingHorizontal: HP, paddingVertical: sw(16),
    backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0',
  },
  greeting: { fontSize: sf(18), fontWeight: '700', color: '#1e293b' },
  headerSub: { fontSize: sf(13), color: '#64748b', marginTop: sw(2) },
  iconCircle: {
    width: sw(40), height: sw(40), borderRadius: sw(20),
    backgroundColor: '#eff6ff', justifyContent: 'center', alignItems: 'center',
  },
  list: { padding: sw(16), gap: sw(12) },
  card: {
    backgroundColor: '#fff', borderRadius: sw(14), padding: sw(16),
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06, shadowRadius: 4, elevation: 2,
  },
  cardHeader: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: sw(8) },
  categoryBadge: { borderRadius: sw(6), paddingHorizontal: sw(8), paddingVertical: sw(2) },
  categoryText: { fontSize: sf(11), fontWeight: '700', textTransform: 'uppercase' },
  dateText: { fontSize: sf(12), color: '#94a3b8' },
  cardTitle: { fontSize: sf(16), fontWeight: '700', color: '#1e293b', marginBottom: sw(4) },
  cardContent: { fontSize: sf(14), color: '#475569', lineHeight: sw(20), marginTop: sw(8) },
  expandHint: { fontSize: sf(12), color: '#176b87', marginTop: sw(8) },
  errorBox: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: sw(24) },
  errorText: { color: '#64748b', fontSize: sf(14), textAlign: 'center', marginTop: sw(8) },
  retryBtn: { marginTop: sw(16), paddingHorizontal: HP, paddingVertical: sw(10), backgroundColor: '#176b87', borderRadius: sw(8) },
  retryText: { color: '#fff', fontWeight: '600' },
  empty: { flex: 1, alignItems: 'center', paddingTop: sw(80) },
  emptyText: { color: '#94a3b8', fontSize: sf(15), marginTop: sw(12) },
});
