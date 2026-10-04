// src/screens/main/EncomendasScreen.tsx
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  View, FlatList, StyleSheet, TouchableOpacity,
  Modal, TextInput, ScrollView, ActivityIndicator, RefreshControl,
  Alert, Platform,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';

import { useAuth } from '../../context/AuthContext';
import {
  createDelivery,
  deleteDelivery,
  Delivery,
  getDeliveries,
  getDeliveriesByResident,
  getFriendlyErrorMessage,
  getUnitsByCondominium,
  getUsersByCondominium,
  pickupDelivery,
  Unit,
  updateDelivery,
  User,
} from '../../api/client';

import { theme } from '../../theme/theme';
import AppText from '../../components/AppText';
import BackButton from '../../components/BackButton';
function canManageDeliveries(role?: string) {
  return ['admin', 'sindico', 'porteiro'].includes((role ?? '').toLowerCase());
}

function formatUnit(unit?: Unit) {
  if (!unit) return '';
  return [unit.block, unit.number].filter(Boolean).join(' ') || unit.id;
}

function emptyForm() {
  return {
    trackingCode: '',
    description: '',
    type: 'encomenda',
    residentId: '',
    unitId: '',
    status: 'pending',
    observations: '',
    fotoUrl: '',
  };
}

function statusInfo(status: string) {
  if (status === 'picked_up' || status === 'retirado') {
    return { bg: theme.color.successSoft, text: theme.color.success, label: 'Retirado' };
  }
  if (status === 'returned' || status === 'devolvido') {
    return { bg: theme.color.border, text: theme.color.inkMuted, label: 'Devolvido' };
  }
  return { bg: theme.color.warningSoft, text: theme.color.warning, label: 'Pendente' };
}

export default function EncomendasScreen() {
  const { session } = useAuth();
  const canManage = canManageDeliveries(session?.role);

  const [encomendas, setEncomendas] = useState<Delivery[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [units, setUnits] = useState<Unit[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [listError, setListError] = useState('');
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [moradorModalVisible, setMoradorModalVisible] = useState(false);
  const [form, setForm] = useState(emptyForm);

  const residents = useMemo(
    () => users.filter(user => ['morador', 'sindico'].includes((user.role ?? '').toLowerCase())),
    [users],
  );

  const loadEncomendas = useCallback(async (silent = false) => {
    if (!session?.condominiumId || !session?.userId) return;
    if (!silent) setLoading(true);
    setListError('');

    try {
      const [usersData, unitsData, data] = await Promise.all([
        getUsersByCondominium(session.condominiumId),
        getUnitsByCondominium(session.condominiumId),
        canManage ? getDeliveries() : getDeliveriesByResident(session.userId),
      ]);

      setUsers(usersData);
      setUnits(unitsData);
      setEncomendas(data.sort((a, b) => new Date(b.arrivedAt).getTime() - new Date(a.arrivedAt).getTime()));
    } catch (error: any) {
      console.error('Erro ao carregar encomendas:', error);
      setListError(getFriendlyErrorMessage(error, 'Não foi possível carregar as encomendas.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [canManage, session?.condominiumId, session?.userId]);

  useFocusEffect(useCallback(() => { loadEncomendas(); }, [loadEncomendas]));
  useEffect(() => { loadEncomendas(); }, [loadEncomendas]);

  const onRefresh = () => {
    setRefreshing(true);
    loadEncomendas(true);
  };

  const resetForm = () => {
    setEditingId(null);
    setForm(emptyForm());
    setFormError('');
  };

  const unitName = (id: string) =>
    formatUnit(units.find(unit => unit.id === id)) || id || '-';

  const residentName = (id: string) =>
    users.find(user => user.id === id)?.name ?? (id === session?.userId ? session?.userName : id) ?? '-';

  const handleSelectMorador = (morador: User) => {
    setForm(prev => ({
      ...prev,
      residentId: morador.id,
      unitId: morador.unitId || prev.unitId,
    }));
    setMoradorModalVisible(false);
  };

  const handleSave = async () => {
    if (saving) return;
    if (!canManage) return;
    setFormError('');

    if (!form.trackingCode.trim()) { setFormError('Informe o código de rastreamento.'); return; }
    if (!form.residentId) { setFormError('Selecione o morador.'); return; }

    const isEditing = Boolean(editingId);
    setSaving(true);
    try {
      const payload = {
        trackingCode: form.trackingCode.trim(),
        description: form.description.trim(),
        type: form.type,
        residentId: form.residentId,
        unitId: form.unitId,
        status: form.status,
        observations: form.observations.trim(),
        fotoUrl: form.fotoUrl.trim(),
      };

      if (editingId) {
        await updateDelivery(editingId, payload);
      } else {
        await createDelivery(payload);
      }

      const successMessage = isEditing ? 'Encomenda atualizada com sucesso.' : 'Encomenda criada com sucesso.';
      const finishSave = async () => {
        await loadEncomendas(true);
        setModalVisible(false);
        resetForm();
      };

      if (Platform.OS === 'web') {
        window.alert(successMessage);
        await finishSave();
        return;
      }

      Alert.alert('Sucesso', successMessage, [
        {
          text: 'OK',
          onPress: finishSave,
        },
      ]);
    } catch (error: any) {
      console.error('Erro ao salvar encomenda:', error);
      setFormError(getFriendlyErrorMessage(error, 'Não foi possível salvar a encomenda.'));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = (id: string) => {
    Alert.alert('Remover encomenda', 'Deseja remover esta encomenda?', [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          try {
            await deleteDelivery(id);
            await loadEncomendas(true);
            Alert.alert('Sucesso', 'Encomenda removida com sucesso.');
          } catch (error) {
            console.error('Erro ao remover encomenda:', error);
            Alert.alert('Erro', getFriendlyErrorMessage(error, 'Não foi possível remover a encomenda.'));
          }
        },
      },
    ]);
  };

  const handleEdit = (item: Delivery) => {
    setEditingId(item.id);
    setForm({
      trackingCode: item.trackingCode ?? '',
      description: item.description ?? '',
      type: item.type || 'encomenda',
      residentId: item.residentId ?? '',
      unitId: item.unitId ?? '',
      status: item.status || 'pending',
      observations: item.observations ?? '',
      fotoUrl: item.fotoUrl ?? '',
    });
    setFormError('');
    setModalVisible(true);
  };

  const handlePickup = async (id: string) => {
    try {
      await pickupDelivery(id, session?.userName || 'Morador');
      loadEncomendas(true);
    } catch (error) {
      console.error('Erro ao registrar retirada:', error);
      Alert.alert('Erro', getFriendlyErrorMessage(error, 'Não foi possível registrar a retirada.'));
    }
  };

  const renderEncomenda = ({ item }: { item: Delivery }) => {
    const info = statusInfo(item.status);
    const pickedDate = item.pickedUpAt
      ? new Date(item.pickedUpAt).toLocaleDateString('pt-BR')
      : null;

    return (
      <View style={styles.card}>
        <View style={styles.cardRow}>
          <AppText style={styles.cardTracking}>{item.trackingCode}</AppText>
          <View style={[styles.badge, { backgroundColor: info.bg }]}>
            <AppText style={[styles.badgeText, { color: info.text }]}>{info.label}</AppText>
          </View>
        </View>
        <AppText style={styles.cardDesc}>{item.description || item.type}</AppText>
        <AppText style={styles.cardMeta}>Morador: {residentName(item.residentId)}</AppText>
        <AppText style={styles.cardMeta}>Unidade: {unitName(item.unitId)}</AppText>
        <AppText style={styles.cardMeta}>
          Chegou em: {item.arrivedAt ? new Date(item.arrivedAt).toLocaleDateString('pt-BR') : '-'}
        </AppText>
        {pickedDate ? <AppText style={styles.cardMeta}>Retirado em: {pickedDate}</AppText> : null}
        {item.observations ? <AppText style={styles.cardObs}>{item.observations}</AppText> : null}
        <View style={styles.actionRow}>
          {canManage ? (
            <>
              <TouchableOpacity accessibilityRole="button" onPress={() => handleEdit(item)} style={styles.actionBtn}>
                <Ionicons name="create-outline" size={18} color={theme.color.action} />
                <AppText style={{ color: theme.color.action }}>Editar</AppText>
              </TouchableOpacity>
              <TouchableOpacity accessibilityRole="button" onPress={() => handleDelete(item.id)} style={styles.actionBtn}>
                <Ionicons name="trash-outline" size={18} color={theme.color.danger} />
                <AppText style={{ color: theme.color.danger }}>Remover</AppText>
              </TouchableOpacity>
            </>
          ) : null}
          {item.status !== 'picked_up' && item.status !== 'retirado' ? (
            <TouchableOpacity accessibilityRole="button" onPress={() => handlePickup(item.id)} style={styles.actionBtn}>
              <Ionicons name="checkmark-circle-outline" size={18} color={theme.color.success} />
              <AppText style={{ color: theme.color.success }}>Retirar</AppText>
            </TouchableOpacity>
          ) : null}
        </View>
      </View>
    );
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <BackButton />
        <AppText style={styles.title}>Encomendas</AppText>
        {canManage ? (
          <TouchableOpacity accessibilityRole="button" style={styles.addBtn} onPress={() => { resetForm(); setModalVisible(true); }}>
            <Ionicons name="add" size={22} color="#fff" />
            <AppText style={styles.addBtnText}>Nova</AppText>
          </TouchableOpacity>
        ) : null}
      </View>

      {loading && !refreshing ? (
        <ActivityIndicator size="large" color={theme.color.action} style={{ marginTop: 60 }} />
      ) : listError ? (
        <View style={styles.errorBox}>
          <Ionicons name="alert-circle-outline" size={32} color={theme.color.danger} />
          <AppText style={styles.errorText}>{listError}</AppText>
          <TouchableOpacity accessibilityRole="button" style={styles.retryBtn} onPress={() => loadEncomendas()}>
            <AppText style={styles.retryText}>Tentar novamente</AppText>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={encomendas}
          keyExtractor={item => item.id}
          renderItem={renderEncomenda}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={[theme.color.action]} />}
          contentContainerStyle={styles.list}
          ListEmptyComponent={
            <View style={styles.empty}>
              <Ionicons name="gift-outline" size={48} color={theme.color.borderStrong} />
              <AppText style={styles.emptyText}>Nenhuma encomenda encontrada.</AppText>
              <AppText style={styles.emptySubtext}>
                {canManage ? 'Toque em "Nova" para adicionar.' : 'Quando chegar uma encomenda, ela aparecera aqui.'}
              </AppText>
            </View>
          }
        />
      )}

      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <AppText style={styles.modalTitle}>{editingId ? 'Editar Encomenda' : 'Nova Encomenda'}</AppText>
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

            <AppText style={styles.label}>Codigo *</AppText>
            <TextInput accessibilityLabel="Codigo" style={styles.input} value={form.trackingCode} onChangeText={trackingCode => setForm({ ...form, trackingCode })} />

            <AppText style={styles.label}>Tipo</AppText>
            <View style={styles.chipRow}>
              {['encomenda', 'lanche'].map(type => (
                <TouchableOpacity accessibilityRole="button"
                  key={type}
                  style={[styles.areaChip, form.type === type && styles.areaChipActive]}
                  onPress={() => setForm({ ...form, type })}
                >
                  <AppText style={[styles.areaChipText, form.type === type && { color: '#fff' }]}>
                    {type === 'lanche' ? 'Lanche' : 'Encomenda'}
                  </AppText>
                </TouchableOpacity>
              ))}
            </View>

            <AppText style={styles.label}>Descricao</AppText>
            <TextInput accessibilityLabel="Descricao" style={styles.input} value={form.description} onChangeText={description => setForm({ ...form, description })} />

            <AppText style={styles.label}>Morador *</AppText>
            <TouchableOpacity accessibilityRole="button" style={styles.selectMorador} onPress={() => setMoradorModalVisible(true)}>
              <AppText style={form.residentId ? styles.selectMoradorText : styles.selectMoradorPlaceholder}>
                {form.residentId ? residentName(form.residentId) : 'Selecione um morador'}
              </AppText>
              <Ionicons name="chevron-down" size={20} color={theme.color.inkMuted} />
            </TouchableOpacity>

            <AppText style={styles.label}>Unidade</AppText>
            {units.length ? (
              <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 12 }}>
                {units.map(unit => (
                  <TouchableOpacity accessibilityRole="button"
                    key={unit.id}
                    style={[styles.areaChip, form.unitId === unit.id && styles.areaChipActive]}
                    onPress={() => setForm({ ...form, unitId: unit.id })}
                  >
                    <AppText style={[styles.areaChipText, form.unitId === unit.id && { color: '#fff' }]}>
                      {formatUnit(unit)}
                    </AppText>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            ) : (
              <AppText style={styles.helpText}>Nenhuma unidade cadastrada para este condominio.</AppText>
            )}

            <AppText style={styles.label}>Status</AppText>
            <View style={styles.chipRow}>
              <TouchableOpacity accessibilityRole="button" style={[styles.areaChip, form.status === 'pending' && styles.areaChipActive]} onPress={() => setForm({ ...form, status: 'pending' })}>
                <AppText style={form.status === 'pending' ? { color: '#fff' } : {}}>Pendente</AppText>
              </TouchableOpacity>
              <TouchableOpacity accessibilityRole="button" style={[styles.areaChip, form.status === 'picked_up' && styles.areaChipActive]} onPress={() => setForm({ ...form, status: 'picked_up' })}>
                <AppText style={form.status === 'picked_up' ? { color: '#fff' } : {}}>Retirado</AppText>
              </TouchableOpacity>
            </View>

            <AppText style={styles.label}>Observacoes</AppText>
            <TextInput accessibilityLabel="Observacoes"
              style={[styles.input, { height: 80, textAlignVertical: 'top' }]}
              multiline
              value={form.observations}
              onChangeText={observations => setForm({ ...form, observations })}
            />

            <AppText style={styles.label}>Foto URL/Base64</AppText>
            <TextInput accessibilityLabel="Foto URL/Base64" style={styles.input} value={form.fotoUrl} onChangeText={fotoUrl => setForm({ ...form, fotoUrl })} />

            <TouchableOpacity accessibilityRole="button" style={styles.saveBtn} onPress={handleSave} disabled={saving}>
              {saving ? <ActivityIndicator color="#fff" /> : <AppText style={styles.saveBtnText}>Salvar</AppText>}
            </TouchableOpacity>
          </ScrollView>
        </SafeAreaView>
      </Modal>

      <Modal visible={moradorModalVisible} animationType="slide" transparent>
        <View style={styles.moradorModalOverlay}>
          <View style={styles.moradorModalContent}>
            <View style={styles.modalHeader}>
              <AppText style={styles.modalTitle}>Selecione um morador</AppText>
              <TouchableOpacity accessibilityRole="button" onPress={() => setMoradorModalVisible(false)}>
                <Ionicons name="close" size={26} color={theme.color.inkMuted} />
              </TouchableOpacity>
            </View>
            <FlatList
              data={residents}
              keyExtractor={item => item.id}
              renderItem={({ item }) => (
                <TouchableOpacity accessibilityRole="button" style={styles.moradorItem} onPress={() => handleSelectMorador(item)}>
                  <AppText style={styles.moradorName}>{item.name}</AppText>
                  <AppText style={styles.moradorUnit}>Unidade {item.unitId ? unitName(item.unitId) : '-'}</AppText>
                </TouchableOpacity>
              )}
              ItemSeparatorComponent={() => <View style={styles.separator} />}
              ListEmptyComponent={
                <AppText style={styles.modalEmpty}>Nenhum morador encontrado.</AppText>
              }
            />
          </View>
        </View>
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
  list: { padding: 16, gap: 12, paddingBottom: 80 },
  card: { backgroundColor: '#fff', borderRadius: 14, padding: 16, shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 4, elevation: 2 },
  cardRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6, gap: 8 },
  cardTracking: { fontSize: 16, fontWeight: '700', color: theme.color.ink, flex: 1 },
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 2 },
  badgeText: { fontSize: 11, fontWeight: '700' },
  cardDesc: { fontSize: 15, fontWeight: '500', marginVertical: 4, color: theme.color.ink },
  cardMeta: { fontSize: 13, color: theme.color.inkMuted, marginTop: 2 },
  cardObs: { fontSize: 13, color: theme.color.inkMuted, marginTop: 6, fontStyle: 'italic' },
  actionRow: { flexDirection: 'row', justifyContent: 'flex-start', flexWrap: 'wrap', gap: 12, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: theme.color.border },
  actionBtn: { flexDirection: 'row', alignItems: 'center', gap: 4, backgroundColor: theme.color.surfaceSoft, paddingHorizontal: 12, paddingVertical: 6, borderRadius: 20 },
  empty: { alignItems: 'center', paddingTop: 80, paddingHorizontal: 24 },
  emptyText: { color: theme.color.inkMuted, fontSize: 15, marginTop: 12, fontWeight: '600' },
  emptySubtext: { color: theme.color.borderStrong, fontSize: 13, marginTop: 6, textAlign: 'center' },
  errorBox: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 24 },
  errorText: { color: theme.color.inkMuted, fontSize: 14, textAlign: 'center', marginTop: 8 },
  retryBtn: { marginTop: 16, paddingHorizontal: 20, paddingVertical: 10, backgroundColor: theme.color.action, borderRadius: 8 },
  retryText: { color: '#fff', fontWeight: '600' },
  modal: { flex: 1, backgroundColor: theme.color.background },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: theme.color.border },
  modalTitle: { fontSize: 18, fontWeight: '700', color: theme.color.ink },
  modalBody: { padding: 20 },
  formError: {
    flexDirection: 'row', alignItems: 'center', gap: 6,
    backgroundColor: theme.color.dangerSoft, borderRadius: 8, padding: 12, marginBottom: 8,
  },
  formErrorText: { color: theme.color.danger, fontSize: 13, flex: 1 },
  label: { fontSize: 13, fontWeight: '600', color: theme.color.inkMuted, marginBottom: 6, marginTop: 12 },
  input: { borderWidth: 1, borderColor: theme.color.border, borderRadius: 10, padding: 12, fontSize: 15, backgroundColor: '#fff', marginBottom: 4 },
  chipRow: { flexDirection: 'row', gap: 8, marginBottom: 4, flexWrap: 'wrap' },
  areaChip: { borderWidth: 1, borderColor: theme.color.border, borderRadius: 20, paddingHorizontal: 14, paddingVertical: 8, marginRight: 8, backgroundColor: '#fff' },
  areaChipActive: { backgroundColor: theme.color.action, borderColor: theme.color.action },
  areaChipText: { fontSize: 14, color: theme.color.inkMuted, fontWeight: '600' },
  helpText: { color: theme.color.danger, fontSize: 13, marginBottom: 8 },
  saveBtn: { backgroundColor: theme.color.action, borderRadius: 12, padding: 16, alignItems: 'center', marginTop: 24, marginBottom: 40 },
  saveBtnText: { color: '#fff', fontSize: 16, fontWeight: '700' },
  selectMorador: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: theme.color.border,
    borderRadius: 10,
    padding: 12,
    backgroundColor: '#fff',
    marginBottom: 4,
  },
  selectMoradorText: { fontSize: 15, color: theme.color.ink },
  selectMoradorPlaceholder: { fontSize: 15, color: theme.color.inkMuted },
  moradorModalOverlay: { flex: 1, backgroundColor: '#00000080', justifyContent: 'center', padding: 20 },
  moradorModalContent: { backgroundColor: '#fff', borderRadius: 20, maxHeight: '80%' },
  moradorItem: { padding: 16, flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 12 },
  moradorName: { fontSize: 16, fontWeight: '500', color: theme.color.ink, flex: 1 },
  moradorUnit: { fontSize: 14, color: theme.color.inkMuted },
  modalEmpty: { padding: 20, color: theme.color.inkMuted, textAlign: 'center' },
  separator: { height: 1, backgroundColor: theme.color.border },
});
