import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  ActivityIndicator, Alert, FlatList, Modal, RefreshControl, ScrollView,
  Platform, StyleSheet, Text, TextInput, TouchableOpacity, View,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';

import {
  checkInVisitor, checkOutVisitor, Condominium, createVisitor, deleteVisitor,
  getCondominiums, getUnitsByCondominium, getUsers, getVisitors,
  getVisitorsByResident, getFriendlyErrorMessage, Unit, updateVisitor, User, Visitor,
} from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { maskPhoneBR, onlyDigits } from '../../utils/formatters';

const STATUSES = ['checked-in', 'checked-out'];

function canManageVisitors(role?: string) {
  return ['admin', 'sindico'].includes((role ?? '').toLowerCase());
}

function emptyForm(condominiumId = '', residentId = '') {
  return {
    condominiumId, fullName: '', document: '', phone: '', residentId,
    unitId: '', reason: '', status: 'checked-in',
  };
}

function unitLabel(unit?: Unit) {
  if (!unit) return '-';
  return [unit.block, unit.number].filter(Boolean).join(' ') || unit.id;
}

function formatDateTime(value?: string) {
  if (!value) return '-';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('pt-BR');
}

function visitorId(visitor: Visitor) {
  const mongoId = typeof visitor._id === 'object' ? visitor._id?.$oid : visitor._id;
  return String(visitor.id ?? mongoId ?? visitor.visitorId ?? '').trim();
}

export default function VisitantesScreen() {
  const { session } = useAuth();
  const canManage = canManageVisitors(session?.role);
  const [visitors, setVisitors] = useState<Visitor[]>([]);
  const [condominiums, setCondominiums] = useState<Condominium[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [units, setUnits] = useState<Unit[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [listError, setListError] = useState('');
  const [modalVisible, setModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState(() => emptyForm(session?.condominiumId, session?.userId));
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState('');
  const savingRef = useRef(false);
  const deletingRef = useRef(false);

  const condominiumNames = useMemo(() => new Map(condominiums.map(item => [item.id, item.name])), [condominiums]);
  const userNames = useMemo(() => new Map(users.map(item => [item.id, item.name])), [users]);
  const unitNames = useMemo(() => new Map(units.map(item => [item.id, unitLabel(item)])), [units]);

  const loadVisitors = useCallback(async (silent = false) => {
    if (!session?.userId) return;
    if (!silent) setLoading(true);
    setListError('');
    try {
      const data = canManage ? await getVisitors() : await getVisitorsByResident(session.userId);
      setVisitors(data);
      const [condominiumData, userData] = await Promise.all([
        getCondominiums(),
        canManage ? getUsers() : Promise.resolve([]),
      ]);
      setCondominiums(condominiumData);
      setUsers(userData);
    } catch (error: any) {
      console.error('Erro ao carregar visitantes:', error);
      setListError(getFriendlyErrorMessage(error, 'Não foi possível carregar os visitantes.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [canManage, session?.userId]);

  const loadUnits = useCallback(async (condominiumId: string) => {
    if (!condominiumId) { setUnits([]); return; }
    try { setUnits(await getUnitsByCondominium(condominiumId)); }
    catch { setUnits([]); }
  }, []);

  useFocusEffect(useCallback(() => { loadVisitors(); }, [loadVisitors]));
  useEffect(() => { loadUnits(form.condominiumId); }, [form.condominiumId, loadUnits]);

  const closeModal = () => {
    setModalVisible(false);
    setEditingId(null);
    setForm(emptyForm(session?.condominiumId, session?.userId));
    setFormError('');
  };

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm(session?.condominiumId, session?.userId));
    setFormError('');
    setModalVisible(true);
  };

  const openEdit = (visitor: Visitor) => {
    setEditingId(visitorId(visitor));
    setForm({
      condominiumId: visitor.condominiumId ?? '',
      fullName: visitor.fullName,
      document: visitor.document,
      phone: maskPhoneBR(visitor.phone ?? ''),
      residentId: visitor.residentId,
      unitId: visitor.unitId,
      reason: visitor.reason,
      status: visitor.status,
    });
    setFormError('');
    setModalVisible(true);
  };

  const handleSave = async () => {
    if (savingRef.current) return;

    const residentId = canManage ? form.residentId : session?.userId ?? '';
    const condominiumId = canManage ? form.condominiumId : session?.condominiumId ?? '';
    if (!form.fullName.trim()) { setFormError('Informe o nome completo.'); return; }
    if (!form.document.trim()) { setFormError('Informe o documento.'); return; }
    if (form.phone && onlyDigits(form.phone).length !== 10) { setFormError('Informe um telefone vÃ¡lido no formato 31 9999-2702.'); return; }
    if (!residentId.trim()) { setFormError('Selecione o morador.'); return; }
    if (!form.unitId.trim()) { setFormError('Selecione a unidade.'); return; }
    if (!form.reason.trim()) { setFormError('Informe o motivo da visita.'); return; }
    if (canManage && !condominiumId) {
      setFormError('Selecione um condomínio.');
      return;
    }

    savingRef.current = true;
    setSaving(true);
    setFormError('');
    const payload = {
      condominiumId,
      fullName: form.fullName.trim(),
      document: form.document.trim(),
      phone: form.phone.trim(),
      residentId,
      unitId: form.unitId,
      reason: form.reason.trim(),
      status: canManage ? form.status : undefined,
    };
    try {
      const isEditing = Boolean(editingId);
      if (!editingId) {
        await createVisitor(payload);

        const finishCreate = async () => {
          await loadVisitors(true);
          closeModal();
          savingRef.current = false;
          setSaving(false);
        };

        if (Platform.OS === 'web') {
          window.alert('Visitante criado com sucesso.');
          await finishCreate();
          return;
        }

        Alert.alert('Sucesso', 'Visitante criado com sucesso.', [
          {
            text: 'OK',
            onPress: finishCreate,
          },
        ]);
        return;
      }

      await updateVisitor(editingId, payload);

      const finishUpdate = async () => {
        await loadVisitors(true);
        closeModal();
      };

      if (Platform.OS === 'web') {
        window.alert('Visitante atualizado com sucesso.');
        await finishUpdate();
        savingRef.current = false;
        setSaving(false);
        return;
      }

      Alert.alert('Sucesso', 'Visitante atualizado com sucesso.', [
        {
          text: 'OK',
          onPress: finishUpdate,
        },
      ]);
      savingRef.current = false;
      setSaving(false);
    } catch (error) {
      console.error('Erro ao salvar visitante:', error);
      setFormError(getFriendlyErrorMessage(error, 'Não foi possível salvar o visitante.'));
      savingRef.current = false;
      setSaving(false);
    }
  };

  const runAction = async (visitor: Visitor, action: 'check-in' | 'check-out') => {
    const id = visitorId(visitor);
    if (!id) {
      Alert.alert('Erro', 'Não foi possível identificar o visitante.');
      return;
    }
    try {
      if (action === 'check-in') await checkInVisitor(id);
      else await checkOutVisitor(id);
      await loadVisitors(true);
      Alert.alert('Sucesso', action === 'check-in' ? 'Entrada registrada.' : 'Saída registrada.');
    } catch (error) {
      console.error('Erro ao registrar movimentação do visitante:', error);
      Alert.alert('Erro', getFriendlyErrorMessage(error, 'Não foi possível atualizar o visitante.'));
    }
  };

  const handleDelete = (visitor: Visitor) => {
    const id = visitorId(visitor);
    if (!id) {
      Alert.alert('Erro', 'Não foi possível identificar o visitante.');
      return;
    }

    const removeVisitor = async () => {
      if (deletingRef.current) return;
      deletingRef.current = true;
      setDeletingId(id);
      try {
        await deleteVisitor(id);
        await loadVisitors(true);
        if (Platform.OS === 'web') window.alert('Visitante removido com sucesso.');
        else Alert.alert('Sucesso', 'Visitante removido com sucesso.');
      } catch (error) {
        console.error('Erro ao remover visitante:', error);
        const message = getFriendlyErrorMessage(error, 'Não foi possível remover o visitante.');
        if (Platform.OS === 'web') window.alert(message);
        else Alert.alert('Erro', message);
      } finally {
        deletingRef.current = false;
        setDeletingId('');
      }
    };

    if (Platform.OS === 'web') {
      if (window.confirm(`Deseja remover ${visitor.fullName}?`)) removeVisitor();
      return;
    }

    Alert.alert('Confirmar remoção', `Deseja remover ${visitor.fullName}?`, [
      { text: 'Cancelar', style: 'cancel' },
      { text: 'Remover', style: 'destructive', onPress: removeVisitor },
    ]);
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <View><Text style={styles.title}>Visitantes</Text><Text style={styles.subtitle}>Cadastro, entrada e saída de visitantes</Text></View>
        <TouchableOpacity style={styles.addBtn} onPress={openCreate}><Ionicons name="add" size={22} color="#fff" /><Text style={styles.addBtnText}>Novo</Text></TouchableOpacity>
      </View>

      {loading && !refreshing ? <ActivityIndicator size="large" color="#176b87" style={styles.loader} /> : listError ? (
        <View style={styles.center}><Ionicons name="alert-circle-outline" size={36} color="#ef4444" /><Text style={styles.errorText}>{listError}</Text><TouchableOpacity style={styles.retryBtn} onPress={() => loadVisitors()}><Text style={styles.retryText}>Tentar novamente</Text></TouchableOpacity></View>
      ) : (
        <FlatList
          data={visitors} keyExtractor={(item, index) => visitorId(item) || `${item.document}-${index}`} contentContainerStyle={styles.list}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); loadVisitors(true); }} colors={['#176b87']} />}
          ListEmptyComponent={<View style={styles.center}><Text style={styles.emptyText}>Nenhum visitante cadastrado.</Text></View>}
          renderItem={({ item }) => (
            <View style={styles.card}>
              <View style={styles.cardTop}><Text style={styles.cardTitle}>{item.fullName}</Text><Text style={styles.badge}>{item.status}</Text></View>
              <Text style={styles.cardText}>Documento: {item.document}</Text>
              <Text style={styles.cardText}>Telefone: {item.phone || '-'}</Text>
              <Text style={styles.cardText}>Condomínio: {condominiumNames.get(item.condominiumId ?? '') ?? item.condominiumId ?? '-'}</Text>
              <Text style={styles.cardText}>Unidade: {unitNames.get(item.unitId) ?? item.unitId}</Text>
              <Text style={styles.cardText}>Entrada: {formatDateTime(item.checkInAt)}</Text>
              <Text style={styles.cardText}>Saída: {formatDateTime(item.checkOutAt)}</Text>
              {canManage ? <View style={styles.actions}>
                <Action icon="create-outline" color="#2563eb" label="Editar" onPress={() => openEdit(item)} />
                <Action icon="log-in-outline" color="#15803d" label="Entrada" onPress={() => runAction(item, 'check-in')} />
                <Action icon="log-out-outline" color="#b45309" label="Saída" onPress={() => runAction(item, 'check-out')} />
                <Action icon="trash-outline" color="#dc2626" label={deletingId === visitorId(item) ? 'Removendo...' : 'Remover'} onPress={() => handleDelete(item)} disabled={Boolean(deletingId)} />
              </View> : null}
            </View>
          )}
        />
      )}

      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}><Text style={styles.modalTitle}>{editingId ? 'Editar Visitante' : 'Novo Visitante'}</Text><TouchableOpacity onPress={closeModal}><Ionicons name="close" size={26} color="#64748b" /></TouchableOpacity></View>
          <ScrollView contentContainerStyle={styles.form} keyboardShouldPersistTaps="handled">
            {formError ? <Text style={styles.formError}>{formError}</Text> : null}
            {canManage ? <>
              <Text style={styles.label}>Condomínio *</Text>
              <ScrollView horizontal showsHorizontalScrollIndicator={false}>{condominiums.map(item => <Choice key={item.id} label={item.name} selected={form.condominiumId === item.id} onPress={() => setForm({ ...form, condominiumId: item.id, unitId: '' })} />)}</ScrollView>
            </> : null}
            <Field label="Nome completo *" value={form.fullName} onChangeText={fullName => setForm({ ...form, fullName })} />
            <Field label="Documento *" value={form.document} onChangeText={document => setForm({ ...form, document })} />
            <Field label="Telefone" placeholder="31 9999-2702" keyboardType="phone-pad" maxLength={12} value={form.phone} onChangeText={phone => setForm({ ...form, phone: maskPhoneBR(phone) })} />
            {canManage ? <>
              <Text style={styles.label}>Morador *</Text>
              <ScrollView horizontal showsHorizontalScrollIndicator={false}>{users.map(item => <Choice key={item.id} label={item.name} selected={form.residentId === item.id} onPress={() => setForm({ ...form, residentId: item.id, unitId: item.unitId ?? form.unitId })} />)}</ScrollView>
            </> : null}
            <Text style={styles.label}>Unidade *</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false}>{units.map(item => <Choice key={item.id} label={unitLabel(item)} selected={form.unitId === item.id} onPress={() => setForm({ ...form, unitId: item.id })} />)}</ScrollView>
            <Field label="Motivo *" value={form.reason} onChangeText={reason => setForm({ ...form, reason })} />
            {canManage ? <><Text style={styles.label}>Status</Text><ScrollView horizontal showsHorizontalScrollIndicator={false}>{STATUSES.map(status => <Choice key={status} label={status} selected={form.status === status} onPress={() => setForm({ ...form, status })} />)}</ScrollView></> : null}
            <TouchableOpacity style={[styles.saveBtn, saving && styles.disabled]} onPress={handleSave} disabled={saving}>
              <Text style={styles.saveBtnText}>{saving ? 'Salvando...' : editingId ? 'Atualizar' : 'Salvar'}</Text>
            </TouchableOpacity>
          </ScrollView>
        </SafeAreaView>
      </Modal>
    </SafeAreaView>
  );
}

function Field({ label, ...props }: React.ComponentProps<typeof TextInput> & { label: string }) {
  return <View><Text style={styles.label}>{label}</Text><TextInput {...props} style={styles.input} /></View>;
}
function Choice({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
  return <TouchableOpacity style={[styles.choice, selected && styles.choiceActive]} onPress={onPress}><Text style={[styles.choiceText, selected && styles.choiceTextActive]}>{label}</Text></TouchableOpacity>;
}
function Action({ icon, color, label, onPress, disabled = false }: { icon: string; color: string; label: string; onPress: () => void; disabled?: boolean }) {
  return <TouchableOpacity style={[styles.actionBtn, disabled && styles.disabled]} onPress={onPress} disabled={disabled}><Ionicons name={icon as any} size={17} color={color} /><Text style={{ color, fontWeight: '600' }}>{label}</Text></TouchableOpacity>;
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 20, paddingVertical: 16, gap: 12, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0' },
  title: { fontSize: 20, fontWeight: '700', color: '#1e293b' }, subtitle: { fontSize: 12, color: '#64748b', marginTop: 2, maxWidth: 230 },
  addBtn: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#176b87', borderRadius: 8, paddingHorizontal: 14, paddingVertical: 8, gap: 4 }, addBtnText: { color: '#fff', fontWeight: '700', fontSize: 14 },
  disabled: { opacity: 0.55 },
  loader: { marginTop: 60 }, list: { padding: 16, paddingBottom: 80, gap: 12, flexGrow: 1 },
  card: { backgroundColor: '#fff', borderRadius: 14, padding: 16, shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 4, elevation: 2 },
  cardTop: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 8, marginBottom: 6 }, cardTitle: { fontSize: 16, fontWeight: '700', color: '#1e293b', flex: 1 },
  badge: { color: '#176b87', backgroundColor: '#e0f2fe', borderRadius: 12, paddingHorizontal: 9, paddingVertical: 3, fontSize: 11, fontWeight: '700' },
  cardText: { fontSize: 13, color: '#64748b', marginTop: 3 }, actions: { flexDirection: 'row', flexWrap: 'wrap', gap: 14, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: '#e2e8f0' }, actionBtn: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 }, emptyText: { color: '#94a3b8', fontSize: 15, fontWeight: '600' }, errorText: { color: '#64748b', fontSize: 14, textAlign: 'center', marginTop: 8 },
  retryBtn: { marginTop: 16, paddingHorizontal: 20, paddingVertical: 10, backgroundColor: '#176b87', borderRadius: 8 }, retryText: { color: '#fff', fontWeight: '600' },
  modal: { flex: 1, backgroundColor: '#f8fafc' }, modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0' }, modalTitle: { fontSize: 18, fontWeight: '700', color: '#1e293b' },
  form: { padding: 20, paddingBottom: 40 }, formError: { color: '#dc2626', backgroundColor: '#fef2f2', borderRadius: 8, padding: 12, marginBottom: 8 }, label: { fontSize: 13, fontWeight: '600', color: '#475569', marginBottom: 6, marginTop: 12 },
  input: { borderWidth: 1, borderColor: '#e2e8f0', borderRadius: 10, padding: 12, fontSize: 15, backgroundColor: '#fff' }, choice: { borderWidth: 1, borderColor: '#e2e8f0', borderRadius: 20, paddingHorizontal: 14, paddingVertical: 8, marginRight: 8, backgroundColor: '#fff' }, choiceActive: { backgroundColor: '#176b87', borderColor: '#176b87' }, choiceText: { color: '#475569', fontWeight: '600' }, choiceTextActive: { color: '#fff' },
  saveBtn: { backgroundColor: '#176b87', borderRadius: 12, padding: 16, alignItems: 'center', marginTop: 24 }, saveBtnText: { color: '#fff', fontSize: 16, fontWeight: '700' },
});
