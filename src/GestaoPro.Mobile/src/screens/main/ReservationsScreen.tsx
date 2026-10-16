import React, { useCallback, useState, useRef } from 'react';
import {
  View, FlatList, StyleSheet, TouchableOpacity,
  Modal, TextInput, ScrollView, ActivityIndicator, RefreshControl,
  Alert, Platform,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';

import {
  getMyReservations, createReservation, getCommonAreas,
  deleteReservation, Reservation, CommonArea, getFriendlyErrorMessage,
} from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { dateBRToApi, isValidTime } from '../../utils/formatters';

import { theme } from '../../theme/theme';
import AppText from '../../components/AppText';
import DateTimeField from '../../components/DateTimeField';
const statusColor: Record<string, string> = {
  pendente: theme.color.warning, aprovada: theme.color.success, rejeitada: theme.color.danger, cancelada: theme.color.inkMuted,
};
const statusLabel: Record<string, string> = {
  pendente: 'Pendente', aprovada: 'Aprovada', rejeitada: 'Rejeitada', cancelada: 'Cancelada',
};

export default function ReservationsScreen() {
  const { session } = useAuth();
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [areas, setAreas] = useState<CommonArea[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [refreshing, setRefreshing] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  const [selectedAreaId, setSelectedAreaId] = useState('');
  const [date, setDate] = useState('');
  const [startTime, setStartTime] = useState('');
  const [endTime, setEndTime] = useState('');
  const [observations, setObservations] = useState('');

  async function load(silent = false) {
    if (!silent) setLoading(true);
    setError('');
    try {
      const userId = session?.userId ?? '';
      const [res, ar] = await Promise.all([
        getMyReservations(userId),
        getCommonAreas(),
      ]);
      setReservations(res.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()));
      setAreas(ar);
    } catch (e) {
      console.error('Erro ao carregar reservas:', e);
      setError(getFriendlyErrorMessage(e, 'Não foi possível carregar as reservas.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }

  // Recarrega ao voltar para a aba, sempre com a versão atual de load (sessão em dia).
  const loadRef = useRef(load);
  loadRef.current = load;
  useFocusEffect(useCallback(() => { loadRef.current(); }, []));

  function areaName(id: string) {
    return areas.find(a => a.id === id)?.name ?? id;
  }

  async function handleCreate() {
    if (saving) return;
    setFormError('');
    if (!selectedAreaId) { setFormError('Selecione uma área comum.'); return; }
    const apiDate = dateBRToApi(date);
    if (!apiDate) { setFormError('Informe uma data válida no formato DD/MM/AAAA.'); return; }
    if (!startTime || !endTime) { setFormError('Informe os horários de início e fim.'); return; }
    if (!isValidTime(startTime) || !isValidTime(endTime)) { setFormError('Informe uma hora válida no formato HH:MM.'); return; }
    setSaving(true);
    try {
      await createReservation({
        commonAreaId: selectedAreaId,
        userId: session!.userId,
        condominiumId: session!.condominiumId,
        date: apiDate,
        startTime,
        endTime,
        observations,
      });
      const finishCreate = async () => {
        await load(true);
        setModalVisible(false);
        setSelectedAreaId(''); setDate(''); setStartTime(''); setEndTime(''); setObservations(''); setFormError('');
      };

      if (Platform.OS === 'web') {
        window.alert('Reserva criada com sucesso.');
        await finishCreate();
        return;
      }

      Alert.alert('Sucesso', 'Reserva criada com sucesso.', [
        {
          text: 'OK',
          onPress: finishCreate,
        },
      ]);
    } catch (e) {
      console.error('Erro ao criar reserva:', e);
      setFormError(getFriendlyErrorMessage(e, 'Não foi possível salvar a reserva.'));
    } finally {
      setSaving(false);
    }
  }

  function handleDelete(reservation: Reservation) {
    Alert.alert('Confirmar remoção', 'Deseja realmente remover esta reserva?', [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          try {
            await deleteReservation(reservation.id);
            await load(true);
            Alert.alert('Sucesso', 'Reserva removida com sucesso.');
          } catch (e) {
            console.error('Erro ao remover reserva:', e);
            Alert.alert('Erro', getFriendlyErrorMessage(e, 'Não foi possível remover a reserva.'));
          }
        },
      },
    ]);
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <AppText style={styles.title}>Reservas</AppText>
        <TouchableOpacity accessibilityRole="button" style={styles.addBtn} onPress={() => setModalVisible(true)}>
          <Ionicons name="add" size={22} color="#fff" />
          <AppText style={styles.addBtnText}>Nova</AppText>
        </TouchableOpacity>
      </View>

      {loading ? (
        <ActivityIndicator size="large" color={theme.color.action} style={{ marginTop: 60 }} />
      ) : error ? (
        <View style={styles.errorBox}>
          <Ionicons name="alert-circle-outline" size={32} color={theme.color.danger} />
          <AppText style={styles.errorText}>{error}</AppText>
          <TouchableOpacity accessibilityRole="button" style={styles.retryBtn} onPress={() => load()}>
            <AppText style={styles.retryText}>Tentar novamente</AppText>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={reservations}
          keyExtractor={item => item.id}
          contentContainerStyle={styles.list}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(true); }} colors={[theme.color.action]} />}
          renderItem={({ item }) => {
            const color = statusColor[item.status] ?? theme.color.inkMuted;
            return (
              <View style={styles.card}>
                <View style={styles.cardRow}>
                  <AppText style={styles.cardArea}>{areaName(item.commonAreaId)}</AppText>
                  <View style={[styles.badge, { backgroundColor: color + '20' }]}>
                    <AppText style={[styles.badgeText, { color }]}>{statusLabel[item.status] ?? item.status}</AppText>
                  </View>
                </View>
                <AppText style={styles.cardDate}>
                  {item.date ? new Date(item.date + 'T00:00:00').toLocaleDateString('pt-BR') : ''}{' '}
                  · ⏰ {item.startTime} – {item.endTime}
                </AppText>
                {item.observations ? <AppText style={styles.cardObs}>{item.observations}</AppText> : null}
                <TouchableOpacity accessibilityRole="button" style={styles.deleteBtn} onPress={() => handleDelete(item)}>
                  <Ionicons name="trash-outline" size={18} color={theme.color.danger} />
                  <AppText style={styles.deleteText}>Remover</AppText>
                </TouchableOpacity>
              </View>
            );
          }}
          ListEmptyComponent={
            <View style={styles.empty}>
              <Ionicons name="calendar-outline" size={48} color={theme.color.borderStrong} />
              <AppText style={styles.emptyText}>Nenhuma reserva encontrada.</AppText>
            </View>
          }
        />
      )}

      {/* Modal Nova Reserva */}
      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <AppText style={styles.modalTitle}>Nova Reserva</AppText>
            <TouchableOpacity accessibilityRole="button" onPress={() => setModalVisible(false)}>
              <Ionicons name="close" size={26} color={theme.color.inkMuted} />
            </TouchableOpacity>
          </View>
          <ScrollView style={styles.modalBody} keyboardShouldPersistTaps="handled">
            {formError ? (
              <View style={styles.formError}>
                <Ionicons name="alert-circle-outline" size={16} color={theme.color.danger} />
                <AppText style={styles.formErrorText}>{formError}</AppText>
              </View>
            ) : null}
            <AppText style={styles.label}>Área comum</AppText>
            {areas.length === 0 ? (
              <AppText style={{ color: theme.color.danger, marginBottom: 12 }}>
                Nenhuma área comum cadastrada. Peça ao administrador para cadastrar.
              </AppText>
            ) : (
              <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 12 }}>
                {areas.map(a => (
                  <TouchableOpacity accessibilityRole="button"
                    key={a.id}
                    style={[styles.areaChip, selectedAreaId === a.id && styles.areaChipActive]}
                    onPress={() => setSelectedAreaId(a.id)}
                  >
                    <AppText style={[styles.areaChipText, selectedAreaId === a.id && { color: '#fff' }]}>{a.name}</AppText>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            )}

            <DateTimeField label="Data" mode="date" value={date} onChange={setDate} minimumDate={new Date()} />
            <DateTimeField label="Início" mode="time" value={startTime} onChange={setStartTime} />
            <DateTimeField label="Fim" mode="time" value={endTime} onChange={setEndTime} />

            <AppText style={styles.label}>Observações (opcional)</AppText>
            <TextInput accessibilityLabel="Observações (opcional)"
              style={[styles.input, { height: 80, textAlignVertical: 'top' }]}
              placeholder="Descreva o uso..."
              multiline
              value={observations}
              onChangeText={setObservations}
            />

            <TouchableOpacity accessibilityRole="button" style={styles.saveBtn} onPress={handleCreate} disabled={saving}>
              {saving ? <ActivityIndicator color="#fff" /> : <AppText style={styles.saveBtnText}>Enviar reserva</AppText>}
            </TouchableOpacity>
          </ScrollView>
        </SafeAreaView>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.color.background },
  header: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingHorizontal: 20, paddingVertical: 16,
    backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: theme.color.border,
  },
  title: { fontSize: 20, fontWeight: '700', color: theme.color.ink },
  addBtn: { flexDirection: 'row', alignItems: 'center', backgroundColor: theme.color.action, borderRadius: 8, paddingHorizontal: 14, paddingVertical: 8, gap: 4 },
  addBtnText: { color: '#fff', fontWeight: '700', fontSize: 14 },
  list: { padding: 16, gap: 12 },
  card: {
    backgroundColor: '#fff', borderRadius: 14, padding: 16,
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 4, elevation: 2,
  },
  cardRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6 },
  cardArea: { fontSize: 16, fontWeight: '700', color: theme.color.ink, flex: 1 },
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 2 },
  badgeText: { fontSize: 11, fontWeight: '700' },
  cardDate: { fontSize: 13, color: theme.color.inkMuted },
  cardObs: { fontSize: 13, color: theme.color.inkMuted, marginTop: 6, fontStyle: 'italic' },
  deleteBtn: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: theme.color.border },
  deleteText: { color: theme.color.danger, fontWeight: '600' },
  empty: { alignItems: 'center', paddingTop: 80 },
  emptyText: { color: theme.color.inkMuted, fontSize: 15, marginTop: 12 },
  errorBox: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 24 },
  errorText: { color: theme.color.inkMuted, fontSize: 14, textAlign: 'center', marginTop: 8 },
  retryBtn: { marginTop: 16, paddingHorizontal: 20, paddingVertical: 10, backgroundColor: theme.color.action, borderRadius: 8 },
  retryText: { color: '#fff', fontWeight: '600' },
  modal: { flex: 1, backgroundColor: theme.color.background },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: theme.color.border },
  modalTitle: { fontSize: 18, fontWeight: '700', color: theme.color.ink },
  modalBody: { padding: 20 },
  label: { fontSize: 13, fontWeight: '600', color: theme.color.inkMuted, marginBottom: 6, marginTop: 12 },
  input: { borderWidth: 1, borderColor: theme.color.border, borderRadius: 10, padding: 12, fontSize: 15, backgroundColor: '#fff', marginBottom: 4 },
  areaChip: { borderWidth: 1, borderColor: theme.color.border, borderRadius: 20, paddingHorizontal: 14, paddingVertical: 8, marginRight: 8, backgroundColor: '#fff' },
  areaChipActive: { backgroundColor: theme.color.action, borderColor: theme.color.action },
  areaChipText: { fontSize: 14, color: theme.color.inkMuted, fontWeight: '600' },
  saveBtn: { backgroundColor: theme.color.action, borderRadius: 12, padding: 16, alignItems: 'center', marginTop: 24, marginBottom: 40 },
  saveBtnText: { color: '#fff', fontSize: 16, fontWeight: '700' },
  formError: {
    flexDirection: 'row', alignItems: 'center', gap: 6,
    backgroundColor: theme.color.dangerSoft, borderRadius: 8, padding: 12, marginBottom: 8,
  },
  formErrorText: { color: theme.color.danger, fontSize: 13, flex: 1 },
});
