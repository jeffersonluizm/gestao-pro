import React, { useCallback, useState } from 'react';
import {
  View, FlatList, StyleSheet, RefreshControl,
  TouchableOpacity, ActivityIndicator,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';

import { getAnnouncements, Announcement, getFriendlyErrorMessage } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { sw, sf, HP } from '../../utils/responsive';

import { theme } from '../../theme/theme';
import AppText from '../../components/AppText';
import PublishAnnouncementModal from '../../components/PublishAnnouncementModal';
const categoryColors: Record<string, string> = {
  Manutenção: theme.color.warning,
  Segurança: theme.color.danger,
  Evento: theme.color.brandAccent,
  Financeiro: theme.color.success,
  Geral: theme.color.action,
};

function AnnouncementCard({ item }: { item: Announcement }) {
  const [expanded, setExpanded] = useState(false);
  const color = categoryColors[item.category] ?? theme.color.action;
  const date = item.createdAt ? new Date(item.createdAt).toLocaleDateString('pt-BR') : '';

  return (
    <TouchableOpacity accessibilityRole="button" style={styles.card} onPress={() => setExpanded(v => !v)} activeOpacity={0.85}>
      <View style={styles.cardHeader}>
        <View style={[styles.categoryBadge, { backgroundColor: color + '20' }]}>
          <AppText style={[styles.categoryText, { color }]}>{item.category ?? 'Geral'}</AppText>
        </View>
        <AppText style={styles.dateText}>{date}</AppText>
      </View>
      <AppText style={styles.cardTitle}>{item.title}</AppText>
      {expanded && <AppText style={styles.cardContent}>{item.content}</AppText>}
      <AppText style={styles.expandHint}>{expanded ? 'Ver menos ▲' : 'Ver mais ▼'}</AppText>
    </TouchableOpacity>
  );
}

export default function HomeScreen() {
  const { session } = useAuth();
  const [announcements, setAnnouncements] = useState<Announcement[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');
  const [publishing, setPublishing] = useState(false);
  const canPublish = ['admin', 'sindico'].includes((session?.role ?? '').toLowerCase());

  async function load(silent = false) {
    if (!silent) setLoading(true);
    setError('');
    try {
      const data = await getAnnouncements();
      setAnnouncements(data.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()));
    } catch (e) {
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
          <AppText style={styles.greeting} accessibilityRole="header">Olá, {session?.userName?.split(' ')[0] ?? 'Morador'}</AppText>
          <AppText style={styles.headerSub}>Avisos do condomínio</AppText>
        </View>
        {canPublish && (
          <TouchableOpacity accessibilityRole="button" accessibilityLabel="Publicar aviso" style={styles.publishBtn} onPress={() => setPublishing(true)}>
            <Ionicons name="add" size={sw(20)} color={theme.color.onAction} />
            <AppText style={styles.publishText}>Publicar</AppText>
          </TouchableOpacity>
        )}
      </View>
      {canPublish && session && (
        <PublishAnnouncementModal
          visible={publishing}
          condominiumId={session.condominiumId}
          authorId={session.userId}
          onClose={() => setPublishing(false)}
          onPublished={() => { setPublishing(false); load(true); }}
        />
      )}

      {loading ? (
        <ActivityIndicator size="large" color={theme.color.action} style={{ marginTop: sw(60) }} />
      ) : error ? (
        <View style={styles.errorBox}>
          <Ionicons name="alert-circle-outline" size={sw(32)} color={theme.color.danger} />
          <AppText style={styles.errorText}>{error}</AppText>
          <TouchableOpacity accessibilityRole="button" style={styles.retryBtn} onPress={() => load()}>
            <AppText style={styles.retryText}>Tentar novamente</AppText>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={announcements}
          keyExtractor={item => item.id}
          renderItem={({ item }) => <AnnouncementCard item={item} />}
          contentContainerStyle={styles.list}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(true); }} colors={[theme.color.action]} />}
          ListEmptyComponent={
            <View style={styles.empty}>
              <Ionicons name="megaphone-outline" size={sw(48)} color={theme.color.borderStrong} />
              <AppText style={styles.emptyText}>Nenhum aviso no momento.</AppText>
            </View>
          }
        />
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.color.background },
  header: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingHorizontal: HP, paddingVertical: sw(16),
    backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: theme.color.border,
  },
  greeting: { fontSize: sf(18), fontWeight: '700', color: theme.color.ink },
  headerSub: { fontSize: sf(13), color: theme.color.inkMuted, marginTop: sw(2) },
  publishBtn: { flexDirection: 'row', alignItems: 'center', gap: 4, backgroundColor: theme.color.action, borderRadius: 999, paddingHorizontal: 14, paddingVertical: 8 },
  publishText: { color: theme.color.onAction, fontWeight: '700' },
  iconCircle: {
    width: sw(40), height: sw(40), borderRadius: sw(20),
    backgroundColor: theme.color.actionSoft, justifyContent: 'center', alignItems: 'center',
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
  dateText: { fontSize: sf(12), color: theme.color.inkMuted },
  cardTitle: { fontSize: sf(16), fontWeight: '700', color: theme.color.ink, marginBottom: sw(4) },
  cardContent: { fontSize: sf(14), color: theme.color.inkMuted, lineHeight: sw(20), marginTop: sw(8) },
  expandHint: { fontSize: sf(12), color: theme.color.action, marginTop: sw(8) },
  errorBox: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: sw(24) },
  errorText: { color: theme.color.inkMuted, fontSize: sf(14), textAlign: 'center', marginTop: sw(8) },
  retryBtn: { marginTop: sw(16), paddingHorizontal: HP, paddingVertical: sw(10), backgroundColor: theme.color.action, borderRadius: sw(8) },
  retryText: { color: '#fff', fontWeight: '600' },
  empty: { flex: 1, alignItems: 'center', paddingTop: sw(80) },
  emptyText: { color: theme.color.inkMuted, fontSize: sf(15), marginTop: sw(12) },
});
