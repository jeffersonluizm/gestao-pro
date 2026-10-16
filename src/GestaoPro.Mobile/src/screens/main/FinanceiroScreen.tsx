import React, { useCallback, useState } from 'react';
import { ActivityIndicator, FlatList, RefreshControl, StyleSheet, TouchableOpacity, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Bill, getBillsByUser, getFriendlyErrorMessage, getPaymentsByBill, Payment } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import AppText from '../../components/AppText';
import BackButton from '../../components/BackButton';
import { theme } from '../../theme/theme';
import { billStatus, formatCurrencyBR, formatDateBR, openTotal, Tone } from '../../utils/finance';

const TONE: Record<Tone, { fg: string; bg: string }> = {
  success: { fg: theme.color.success, bg: theme.color.successSoft },
  warning: { fg: theme.color.warning, bg: theme.color.warningSoft },
  danger: { fg: theme.color.danger, bg: theme.color.dangerSoft },
  neutral: { fg: theme.color.inkMuted, bg: theme.color.surfaceSoft },
};

const METHOD: Record<string, string> = { pix: 'Pix', boleto: 'Boleto', cartao: 'Cartão' };

// Financeiro do morador (só leitura): as próprias cobranças e, ao tocar, os pagamentos de cada uma.
export default function FinanceiroScreen() {
  const { session } = useAuth();
  const [bills, setBills] = useState<Bill[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');
  const [openBill, setOpenBill] = useState<string | null>(null);
  const [payments, setPayments] = useState<Record<string, Payment[] | 'erro'>>({});

  const load = useCallback(async (silent = false) => {
    if (!session) return;
    if (!silent) setLoading(true);
    setError('');
    try {
      const data = await getBillsByUser(session.userId);
      setBills([...data].sort((a, b) => b.dueDate.localeCompare(a.dueDate)));
    } catch (err) {
      setError(getFriendlyErrorMessage(err, 'Não foi possível carregar as cobranças.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [session]);

  useFocusEffect(useCallback(() => { load(); }, [load]));

  async function toggle(bill: Bill) {
    if (openBill === bill.id) return setOpenBill(null);
    setOpenBill(bill.id);
    if (payments[bill.id]) return;
    try {
      const data = await getPaymentsByBill(bill.id);
      setPayments(current => ({ ...current, [bill.id]: data }));
    } catch {
      setPayments(current => ({ ...current, [bill.id]: 'erro' }));
    }
  }

  const total = openTotal(bills);

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <BackButton />
        <View style={{ flex: 1 }}>
          <AppText style={styles.title} accessibilityRole="header">Financeiro</AppText>
          <AppText style={styles.subtitle}>Suas cobranças e pagamentos</AppText>
        </View>
      </View>

      {loading ? (
        <ActivityIndicator size="large" color={theme.color.action} style={{ marginTop: 60 }} accessibilityLabel="Carregando cobranças" />
      ) : error ? (
        <View style={styles.center} accessibilityRole="alert">
          <Ionicons name="alert-circle-outline" size={36} color={theme.color.danger} />
          <AppText style={styles.muted}>{error}</AppText>
          <TouchableOpacity accessibilityRole="button" style={styles.retry} onPress={() => load()}>
            <AppText style={styles.retryText}>Tentar novamente</AppText>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={bills}
          keyExtractor={item => item.id}
          contentContainerStyle={styles.list}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(true); }} colors={[theme.color.action]} />}
          ListHeaderComponent={
            <View style={styles.summary} accessible accessibilityLabel={`Em aberto: ${formatCurrencyBR(total)}`}>
              <AppText style={styles.summaryLabel}>Em aberto</AppText>
              <AppText style={styles.summaryValue}>{formatCurrencyBR(total)}</AppText>
            </View>
          }
          ListEmptyComponent={
            <View style={styles.center}>
              <Ionicons name="cash-outline" size={44} color={theme.color.inkMuted} />
              <AppText style={styles.muted}>Nenhuma cobrança por enquanto.</AppText>
            </View>
          }
          renderItem={({ item }) => {
            const status = billStatus(item.status);
            const tone = TONE[status.tone];
            const itemPayments = payments[item.id];
            const expanded = openBill === item.id;
            return (
              <TouchableOpacity
                accessibilityRole="button"
                accessibilityState={{ expanded }}
                accessibilityHint="Mostra os pagamentos desta cobrança"
                style={styles.card}
                onPress={() => toggle(item)}
              >
                <View style={styles.row}>
                  <AppText style={styles.cardTitle}>{item.description}</AppText>
                  <View style={[styles.badge, { backgroundColor: tone.bg }]}>
                    <AppText style={[styles.badgeText, { color: tone.fg }]}>{status.label}</AppText>
                  </View>
                </View>
                <View style={styles.row}>
                  <AppText style={styles.amount}>{formatCurrencyBR(item.amount)}</AppText>
                  <AppText style={styles.muted}>Vence em {formatDateBR(item.dueDate)}</AppText>
                </View>
                {expanded && (
                  <View style={styles.payments}>
                    {itemPayments === undefined ? (
                      <ActivityIndicator color={theme.color.action} />
                    ) : itemPayments === 'erro' ? (
                      <AppText style={{ color: theme.color.danger }}>Não foi possível carregar os pagamentos.</AppText>
                    ) : itemPayments.length === 0 ? (
                      <AppText style={styles.muted}>Nenhum pagamento registrado.</AppText>
                    ) : itemPayments.map(payment => (
                      <View key={payment.id} style={styles.row}>
                        <AppText>{formatDateBR(payment.paymentDate)} · {METHOD[payment.paymentMethod] ?? payment.paymentMethod}</AppText>
                        <AppText style={{ fontWeight: '600' }}>{formatCurrencyBR(payment.amountPaid)}</AppText>
                      </View>
                    ))}
                  </View>
                )}
              </TouchableOpacity>
            );
          }}
        />
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.color.background },
  header: { flexDirection: 'row', alignItems: 'center', paddingHorizontal: 20, paddingVertical: 16, backgroundColor: theme.color.surface, borderBottomWidth: 1, borderBottomColor: theme.color.border },
  title: { fontSize: 20, fontWeight: '700' },
  subtitle: { fontSize: 13, color: theme.color.inkMuted, marginTop: 2 },
  list: { padding: 16, gap: 12 },
  summary: { backgroundColor: theme.color.brand, borderRadius: 16, padding: 20, marginBottom: 4, borderLeftWidth: 4, borderLeftColor: theme.color.brandAccent },
  summaryLabel: { color: theme.color.onBrandMuted, fontSize: 13 },
  summaryValue: { color: theme.color.onBrand, fontSize: 28, fontWeight: '700', marginTop: 4 },
  card: { backgroundColor: theme.color.surface, borderRadius: 14, padding: 16, gap: 8, borderWidth: 1, borderColor: theme.color.border },
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 8 },
  cardTitle: { fontSize: 15, fontWeight: '700', flex: 1 },
  amount: { fontSize: 18, fontWeight: '700', color: theme.color.brand },
  badge: { borderRadius: 999, paddingHorizontal: 10, paddingVertical: 3 },
  badgeText: { fontSize: 12, fontWeight: '700' },
  payments: { borderTopWidth: 1, borderTopColor: theme.color.border, paddingTop: 10, gap: 6 },
  muted: { color: theme.color.inkMuted, textAlign: 'center' },
  center: { alignItems: 'center', padding: 32, gap: 10 },
  retry: { backgroundColor: theme.color.action, borderRadius: 10, paddingHorizontal: 20, paddingVertical: 10 },
  retryText: { color: theme.color.onAction, fontWeight: '700' },
});
