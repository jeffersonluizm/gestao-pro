import React, { useCallback, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, ActivityIndicator,
  Alert, RefreshControl,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';

import { getDeliveries, Delivery, getFriendlyErrorMessage } from '../../api/client';

const statusColor: Record<string, string> = {
  aguardando: '#f59e0b',
  retirado: '#16a34a',
  devolvido: '#94a3b8',
};
const statusLabel: Record<string, string> = {
  aguardando: 'Aguardando retirada',
  retirado: 'Retirado',
  devolvido: 'Devolvido',
};
const typeIcon: Record<string, string> = {
  encomenda: 'cube-outline',
  correspondencia: 'mail-outline',
  documento: 'document-outline',
  alimento: 'fast-food-outline',
};

export default function DeliveriesScreen() {
  const [deliveries, setDeliveries] = useState<Delivery[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  async function load(silent = false) {
    if (!silent) setLoading(true);
    try {
      const data = await getDeliveries();
      setDeliveries(
        data.sort((a, b) => new Date(b.arrivedAt).getTime() - new Date(a.arrivedAt).getTime()),
      );
    } catch (e: any) {
      console.error('Erro ao carregar encomendas:', e);
      Alert.alert('Erro', getFriendlyErrorMessage(e, 'Não foi possível carregar as encomendas.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }

  useFocusEffect(useCallback(() => { load(); }, []));

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Entregas</Text>
        <View style={styles.infoTag}>
          <Ionicons name="information-circle-outline" size={16} color="#176b87" />
          <Text style={styles.infoText}>Portaria notifica ao chegar</Text>
        </View>
      </View>

      {loading ? (
        <ActivityIndicator size="large" color="#176b87" style={{ marginTop: 60 }} />
      ) : (
        <FlatList
          data={deliveries}
          keyExtractor={item => item.id}
          contentContainerStyle={styles.list}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => { setRefreshing(true); load(true); }}
              colors={['#176b87']}
            />
          }
          renderItem={({ item }) => {
            const sColor = statusColor[item.status] ?? '#94a3b8';
            const icon = (typeIcon[item.type] ?? 'cube-outline') as any;
            const arrivedDate = item.arrivedAt
              ? new Date(item.arrivedAt).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' })
              : '';
            const pickedDate = item.pickedUpAt
              ? new Date(item.pickedUpAt).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' })
              : null;

            return (
              <View style={[styles.card, item.status === 'aguardando' && styles.cardHighlight]}>
                <View style={styles.cardLeft}>
                  <View style={[styles.iconBox, { backgroundColor: sColor + '20' }]}>
                    <Ionicons name={icon} size={22} color={sColor} />
                  </View>
                </View>
                <View style={styles.cardContent}>
                  <View style={styles.cardRow}>
                    <Text style={styles.cardType}>{item.description || item.type}</Text>
                    <View style={[styles.badge, { backgroundColor: sColor + '20' }]}>
                      <Text style={[styles.badgeText, { color: sColor }]}>
                        {statusLabel[item.status] ?? item.status}
                      </Text>
                    </View>
                  </View>
                  {item.trackingCode ? (
                    <Text style={styles.tracking}>📦 {item.trackingCode}</Text>
                  ) : null}
                  {item.recipientName ? (
                    <Text style={styles.meta}>Para: {item.recipientName}</Text>
                  ) : null}
                  {item.unitNumber ? (
                    <Text style={styles.meta}>Unidade: {item.unitNumber}</Text>
                  ) : null}
                  <Text style={styles.date}>Chegou em: {arrivedDate}</Text>
                  {pickedDate && (
                    <Text style={styles.date}>Retirado em: {pickedDate}</Text>
                  )}
                </View>
              </View>
            );
          }}
          ListEmptyComponent={
            <View style={styles.empty}>
              <Ionicons name="cube-outline" size={48} color="#cbd5e1" />
              <Text style={styles.emptyText}>Nenhuma entrega registrada.</Text>
              <Text style={styles.emptySubtext}>Novas entregas aparecerão aqui quando chegarem.</Text>
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
    paddingHorizontal: 20, paddingVertical: 16,
    backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0',
  },
  title: { fontSize: 20, fontWeight: '700', color: '#1e293b' },
  infoTag: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  infoText: { fontSize: 12, color: '#176b87' },
  list: { padding: 16, gap: 12 },
  card: {
    backgroundColor: '#fff', borderRadius: 14, padding: 16,
    flexDirection: 'row', gap: 12,
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06, shadowRadius: 4, elevation: 2,
  },
  cardHighlight: {
    borderLeftWidth: 3, borderLeftColor: '#f59e0b',
  },
  cardLeft: { justifyContent: 'flex-start', paddingTop: 2 },
  iconBox: {
    width: 42, height: 42, borderRadius: 12,
    justifyContent: 'center', alignItems: 'center',
  },
  cardContent: { flex: 1 },
  cardRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 4 },
  cardType: { fontSize: 15, fontWeight: '700', color: '#1e293b', flex: 1, marginRight: 8 },
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 2 },
  badgeText: { fontSize: 10, fontWeight: '700' },
  tracking: { fontSize: 12, color: '#64748b', fontFamily: 'monospace', marginBottom: 2 },
  meta: { fontSize: 13, color: '#475569', marginBottom: 1 },
  date: { fontSize: 12, color: '#94a3b8', marginTop: 2 },
  empty: { alignItems: 'center', paddingTop: 80, paddingHorizontal: 32 },
  emptyText: { color: '#94a3b8', fontSize: 15, marginTop: 12, fontWeight: '600' },
  emptySubtext: { color: '#cbd5e1', fontSize: 13, marginTop: 6, textAlign: 'center' },
});
