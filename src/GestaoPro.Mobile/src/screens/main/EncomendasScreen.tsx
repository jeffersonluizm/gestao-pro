// src/screens/main/EncomendasScreen.tsx
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
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
    return { bg: '#dcfce7', text: '#15803d', label: 'Retirado' };
  }
  if (status === 'returned' || status === 'devolvido') {
    return { bg: '#e2e8f0', text: '#64748b', label: 'Devolvido' };
  }
  return { bg: '#fef3c7', text: '#b45309', label: 'Pendente' };
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
          <Text style={styles.cardTracking}>{item.trackingCode}</Text>
          <View style={[styles.badge, { backgroundColor: info.bg }]}>
            <Text style={[styles.badgeText, { color: info.text }]}>{info.label}</Text>
          </View>
        </View>
        <Text style={styles.cardDesc}>{item.description || item.type}</Text>
        <Text style={styles.cardMeta}>Morador: {residentName(item.residentId)}</Text>
        <Text style={styles.cardMeta}>Unidade: {unitName(item.unitId)}</Text>
        <Text style={styles.cardMeta}>
          Chegou em: {item.arrivedAt ? new Date(item.arrivedAt).toLocaleDateString('pt-BR') : '-'}
        </Text>
        {pickedDate ? <Text style={styles.cardMeta}>Retirado em: {pickedDate}</Text> : null}
        {item.observations ? <Text style={styles.cardObs}>{item.observations}</Text> : null}
        <View style={styles.actionRow}>
          {canManage ? (
            <>
              <TouchableOpacity onPress={() => handleEdit(item)} style={styles.actionBtn}>
                <Ionicons name="create-outline" size={18} color="#3b82f6" />
                <Text style={{ color: '#3b82f6' }}>Editar</Text>
              </TouchableOpacity>
              <TouchableOpacity onPress={() => handleDelete(item.id)} style={styles.actionBtn}>
                <Ionicons name="trash-outline" size={18} color="#ef4444" />
                <Text style={{ color: '#ef4444' }}>Remover</Text>
              </TouchableOpacity>
            </>
          ) : null}
          {item.status !== 'picked_up' && item.status !== 'retirado' ? (
            <TouchableOpacity onPress={() => handlePickup(item.id)} style={styles.actionBtn}>
              <Ionicons name="checkmark-circle-outline" size={18} color="#10b981" />
              <Text style={{ color: '#10b981' }}>Retirar</Text>
            </TouchableOpacity>
          ) : null}
        </View>
      </View>
    );
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Encomendas</Text>
        {canManage ? (
          <TouchableOpacity style={styles.addBtn} onPress={() => { resetForm(); setModalVisible(true); }}>
            <Ionicons name="add" size={22} color="#fff" />
            <Text style={styles.addBtnText}>Nova</Text>
          </TouchableOpacity>
        ) : null}
      </View>

      {loading && !refreshing ? (
        <ActivityIndicator size="large" color="#176b87" style={{ marginTop: 60 }} />
      ) : listError ? (
        <View style={styles.errorBox}>
          <Ionicons name="alert-circle-outline" size={32} color="#ef4444" />
          <Text style={styles.errorText}>{listError}</Text>
          <TouchableOpacity style={styles.retryBtn} onPress={() => loadEncomendas()}>
            <Text style={styles.retryText}>Tentar novamente</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={encomendas}
          keyExtractor={item => item.id}
          renderItem={renderEncomenda}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={['#176b87']} />}
          contentContainerStyle={styles.list}
          ListEmptyComponent={
            <View style={styles.empty}>
              <Ionicons name="gift-outline" size={48} color="#cbd5e1" />
              <Text style={styles.emptyText}>Nenhuma encomenda encontrada.</Text>
              <Text style={styles.emptySubtext}>
                {canManage ? 'Toque em "Nova" para adicionar.' : 'Quando chegar uma encomenda, ela aparecera aqui.'}
              </Text>
            </View>
          }
        />
      )}

      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>{editingId ? 'Editar Encomenda' : 'Nova Encomenda'}</Text>
            <TouchableOpacity onPress={() => setModalVisible(false)}>
              <Ionicons name="close" size={26} color="#64748b" />
            </TouchableOpacity>
          </View>
          <ScrollView style={styles.modalBody} keyboardShouldPersistTaps="handled">
            {formError ? (
              <View style={styles.formError}>
                <Ionicons name="alert-circle-outline" size={16} color="#dc2626" />
                <Text style={styles.formErrorText}>{formError}</Text>
              </View>
            ) : null}

            <Text style={styles.label}>Codigo *</Text>
            <TextInput style={styles.input} value={form.trackingCode} onChangeText={trackingCode => setForm({ ...form, trackingCode })} />

            <Text style={styles.label}>Tipo</Text>
            <View style={styles.chipRow}>
              {['encomenda', 'lanche'].map(type => (
                <TouchableOpacity
                  key={type}
                  style={[styles.areaChip, form.type === type && styles.areaChipActive]}
                  onPress={() => setForm({ ...form, type })}
                >
                  <Text style={[styles.areaChipText, form.type === type && { color: '#fff' }]}>
                    {type === 'lanche' ? 'Lanche' : 'Encomenda'}
                  </Text>
                </TouchableOpacity>
              ))}
            </View>

            <Text style={styles.label}>Descricao</Text>
            <TextInput style={styles.input} value={form.description} onChangeText={description => setForm({ ...form, description })} />

            <Text style={styles.label}>Morador *</Text>
            <TouchableOpacity style={styles.selectMorador} onPress={() => setMoradorModalVisible(true)}>
              <Text style={form.residentId ? styles.selectMoradorText : styles.selectMoradorPlaceholder}>
                {form.residentId ? residentName(form.residentId) : 'Selecione um morador'}
              </Text>
              <Ionicons name="chevron-down" size={20} color="#64748b" />
            </TouchableOpacity>

            <Text style={styles.label}>Unidade</Text>
            {units.length ? (
              <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 12 }}>
                {units.map(unit => (
                  <TouchableOpacity
                    key={unit.id}
                    style={[styles.areaChip, form.unitId === unit.id && styles.areaChipActive]}
                    onPress={() => setForm({ ...form, unitId: unit.id })}
                  >
                    <Text style={[styles.areaChipText, form.unitId === unit.id && { color: '#fff' }]}>
                      {formatUnit(unit)}
                    </Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            ) : (
              <Text style={styles.helpText}>Nenhuma unidade cadastrada para este condominio.</Text>
            )}

            <Text style={styles.label}>Status</Text>
            <View style={styles.chipRow}>
              <TouchableOpacity style={[styles.areaChip, form.status === 'pending' && styles.areaChipActive]} onPress={() => setForm({ ...form, status: 'pending' })}>
                <Text style={form.status === 'pending' ? { color: '#fff' } : {}}>Pendente</Text>
              </TouchableOpacity>
              <TouchableOpacity style={[styles.areaChip, form.status === 'picked_up' && styles.areaChipActive]} onPress={() => setForm({ ...form, status: 'picked_up' })}>
                <Text style={form.status === 'picked_up' ? { color: '#fff' } : {}}>Retirado</Text>
              </TouchableOpacity>
            </View>

            <Text style={styles.label}>Observacoes</Text>
            <TextInput
              style={[styles.input, { height: 80, textAlignVertical: 'top' }]}
              multiline
              value={form.observations}
              onChangeText={observations => setForm({ ...form, observations })}
            />

            <Text style={styles.label}>Foto URL/Base64</Text>
            <TextInput style={styles.input} value={form.fotoUrl} onChangeText={fotoUrl => setForm({ ...form, fotoUrl })} />

            <TouchableOpacity style={styles.saveBtn} onPress={handleSave} disabled={saving}>
              {saving ? <ActivityIndicator color="#fff" /> : <Text style={styles.saveBtnText}>Salvar</Text>}
            </TouchableOpacity>
          </ScrollView>
        </SafeAreaView>
      </Modal>

      <Modal visible={moradorModalVisible} animationType="slide" transparent>
        <View style={styles.moradorModalOverlay}>
          <View style={styles.moradorModalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Selecione um morador</Text>
              <TouchableOpacity onPress={() => setMoradorModalVisible(false)}>
                <Ionicons name="close" size={26} color="#64748b" />
              </TouchableOpacity>
            </View>
            <FlatList
              data={residents}
              keyExtractor={item => item.id}
              renderItem={({ item }) => (
                <TouchableOpacity style={styles.moradorItem} onPress={() => handleSelectMorador(item)}>
                  <Text style={styles.moradorName}>{item.name}</Text>
                  <Text style={styles.moradorUnit}>Unidade {item.unitId ? unitName(item.unitId) : '-'}</Text>
                </TouchableOpacity>
              )}
              ItemSeparatorComponent={() => <View style={styles.separator} />}
              ListEmptyComponent={
                <Text style={styles.modalEmpty}>Nenhum morador encontrado.</Text>
              }
            />
          </View>
        </View>
      </Modal>
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
  addBtn: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#176b87', borderRadius: 8, paddingHorizontal: 14, paddingVertical: 8, gap: 4 },
  addBtnText: { color: '#fff', fontWeight: '700', fontSize: 14 },
  list: { padding: 16, gap: 12, paddingBottom: 80 },
  card: { backgroundColor: '#fff', borderRadius: 14, padding: 16, shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 4, elevation: 2 },
  cardRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6, gap: 8 },
  cardTracking: { fontSize: 16, fontWeight: '700', color: '#1e293b', flex: 1 },
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 2 },
  badgeText: { fontSize: 11, fontWeight: '700' },
  cardDesc: { fontSize: 15, fontWeight: '500', marginVertical: 4, color: '#334155' },
  cardMeta: { fontSize: 13, color: '#64748b', marginTop: 2 },
  cardObs: { fontSize: 13, color: '#94a3b8', marginTop: 6, fontStyle: 'italic' },
  actionRow: { flexDirection: 'row', justifyContent: 'flex-start', flexWrap: 'wrap', gap: 12, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: '#e2e8f0' },
  actionBtn: { flexDirection: 'row', alignItems: 'center', gap: 4, backgroundColor: '#f1f5f9', paddingHorizontal: 12, paddingVertical: 6, borderRadius: 20 },
  empty: { alignItems: 'center', paddingTop: 80, paddingHorizontal: 24 },
  emptyText: { color: '#94a3b8', fontSize: 15, marginTop: 12, fontWeight: '600' },
  emptySubtext: { color: '#cbd5e1', fontSize: 13, marginTop: 6, textAlign: 'center' },
  errorBox: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 24 },
  errorText: { color: '#64748b', fontSize: 14, textAlign: 'center', marginTop: 8 },
  retryBtn: { marginTop: 16, paddingHorizontal: 20, paddingVertical: 10, backgroundColor: '#176b87', borderRadius: 8 },
  retryText: { color: '#fff', fontWeight: '600' },
  modal: { flex: 1, backgroundColor: '#f8fafc' },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0' },
  modalTitle: { fontSize: 18, fontWeight: '700', color: '#1e293b' },
  modalBody: { padding: 20 },
  formError: {
    flexDirection: 'row', alignItems: 'center', gap: 6,
    backgroundColor: '#fef2f2', borderRadius: 8, padding: 12, marginBottom: 8,
  },
  formErrorText: { color: '#dc2626', fontSize: 13, flex: 1 },
  label: { fontSize: 13, fontWeight: '600', color: '#475569', marginBottom: 6, marginTop: 12 },
  input: { borderWidth: 1, borderColor: '#e2e8f0', borderRadius: 10, padding: 12, fontSize: 15, backgroundColor: '#fff', marginBottom: 4 },
  chipRow: { flexDirection: 'row', gap: 8, marginBottom: 4, flexWrap: 'wrap' },
  areaChip: { borderWidth: 1, borderColor: '#e2e8f0', borderRadius: 20, paddingHorizontal: 14, paddingVertical: 8, marginRight: 8, backgroundColor: '#fff' },
  areaChipActive: { backgroundColor: '#176b87', borderColor: '#176b87' },
  areaChipText: { fontSize: 14, color: '#475569', fontWeight: '600' },
  helpText: { color: '#ef4444', fontSize: 13, marginBottom: 8 },
  saveBtn: { backgroundColor: '#176b87', borderRadius: 12, padding: 16, alignItems: 'center', marginTop: 24, marginBottom: 40 },
  saveBtnText: { color: '#fff', fontSize: 16, fontWeight: '700' },
  selectMorador: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#e2e8f0',
    borderRadius: 10,
    padding: 12,
    backgroundColor: '#fff',
    marginBottom: 4,
  },
  selectMoradorText: { fontSize: 15, color: '#1e293b' },
  selectMoradorPlaceholder: { fontSize: 15, color: '#94a3b8' },
  moradorModalOverlay: { flex: 1, backgroundColor: '#00000080', justifyContent: 'center', padding: 20 },
  moradorModalContent: { backgroundColor: '#fff', borderRadius: 20, maxHeight: '80%' },
  moradorItem: { padding: 16, flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 12 },
  moradorName: { fontSize: 16, fontWeight: '500', color: '#1e293b', flex: 1 },
  moradorUnit: { fontSize: 14, color: '#64748b' },
  modalEmpty: { padding: 20, color: '#64748b', textAlign: 'center' },
  separator: { height: 1, backgroundColor: '#e2e8f0' },
});
