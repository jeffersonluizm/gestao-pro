import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  FlatList,
  Modal,
  Platform,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';

import {
  Condominium,
  createUser,
  deactivateUser,
  getCondominiums,
  getUnitsByCondominium,
  getUsers,
  getFriendlyErrorMessage,
  Unit,
  updateUser,
  User,
} from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { apiDateToBR, dateBRToApi, isValidCpfFormat, maskCpf, maskDateBR, maskPhoneBR, onlyDigits } from '../../utils/formatters';
import { isStrongPassword, WEAK_PASSWORD_MESSAGE } from '../../utils/passwordPolicy';

const ROLES = [
  { value: 'admin', label: 'Administrador' },
  { value: 'sindico', label: 'Síndico' },
  { value: 'porteiro', label: 'Porteiro' },
  { value: 'morador', label: 'Morador' },
];

function emptyForm(condominiumId = '') {
  return {
    condominiumId,
    unitId: '',
    name: '',
    email: '',
    password: '',
    phone: '',
    cpf: '',
    role: 'morador',
    birthDate: '',
  };
}

function canManageUsers(role?: string) {
  const normalizedRole = (role ?? '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
  return ['admin', 'administrador', 'sindico'].includes(normalizedRole);
}

function userId(user: User) {
  const mongoId = typeof user._id === 'object' ? user._id?.$oid : user._id;
  return [user.id, mongoId, user.userId, user.usuarioId]
    .map(value => String(value ?? '').trim())
    .find(Boolean) ?? '';
}

function normalizeBirthDate(value: string) {
  if (!value.trim()) return undefined;
  return dateBRToApi(value.trim());
}

function formatBirthDate(value?: string) {
  return apiDateToBR(value);
}

function unitLabel(unit?: Unit) {
  if (!unit) return '-';
  return [unit.block, unit.number].filter(Boolean).join(' ') || unit.id;
}

export default function UsuariosScreen() {
  const { session } = useAuth();
  const canManage = canManageUsers(session?.role);
  const [users, setUsers] = useState<User[]>([]);
  const [condominiums, setCondominiums] = useState<Condominium[]>([]);
  const [units, setUnits] = useState<Unit[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [listError, setListError] = useState('');
  const [modalVisible, setModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState(() => emptyForm(session?.condominiumId));
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const savingRef = useRef(false);

  const allUnits = useMemo(() => {
    const map = new Map(units.map(unit => [unit.id, unit]));
    return map;
  }, [units]);

  const loadUsers = useCallback(async (silent = false) => {
    if (!canManage) {
      setLoading(false);
      return;
    }
    if (!silent) setLoading(true);
    setListError('');
    try {
      const [userData, condominiumData] = await Promise.all([getUsers(), getCondominiums()]);
      setUsers(userData);
      setCondominiums(condominiumData);
    } catch (error: any) {
      console.error('Erro ao carregar usuários:', error);
      setListError(getFriendlyErrorMessage(error, 'Não foi possível carregar os usuários.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [canManage]);

  const loadUnits = useCallback(async (condominiumId: string) => {
    if (!condominiumId) {
      setUnits([]);
      return;
    }
    try {
      setUnits(await getUnitsByCondominium(condominiumId));
    } catch {
      setUnits([]);
    }
  }, []);

  useFocusEffect(useCallback(() => { loadUsers(); }, [loadUsers]));
  useEffect(() => { loadUnits(form.condominiumId); }, [form.condominiumId, loadUnits]);

  const closeModal = () => {
    setModalVisible(false);
    setEditingId(null);
    setForm(emptyForm(session?.condominiumId));
    setFormError('');
  };

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm(session?.condominiumId));
    setFormError('');
    setModalVisible(true);
  };

  const openEdit = (user: User) => {
    setEditingId(user.id);
    setForm({
      condominiumId: user.condominiumId,
      unitId: user.unitId ?? '',
      name: user.name,
      email: user.email,
      password: '',
      phone: maskPhoneBR(user.phone),
      cpf: maskCpf(user.cpf),
      role: ROLES.some(role => role.value === user.role) ? user.role : '',
      birthDate: formatBirthDate(user.birthDate),
    });
    setFormError('');
    setModalVisible(true);
  };

  const handleSave = async () => {
    if (savingRef.current) return;
    if (!canManage) return;
    if (!form.condominiumId.trim()) { setFormError('Selecione o condomínio.'); return; }
    if (!form.name.trim()) { setFormError('Informe o nome.'); return; }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) { setFormError('Informe um e-mail válido.'); return; }
    if (!form.phone.trim()) { setFormError('Informe o telefone.'); return; }
    if (onlyDigits(form.phone).length !== 10) { setFormError('Informe um telefone válido no formato 31 9999-2702.'); return; }
    if (!form.role.trim()) { setFormError('Selecione o perfil.'); return; }
    if (!editingId && form.password && !isStrongPassword(form.password)) { setFormError(WEAK_PASSWORD_MESSAGE); return; }
    if (!isValidCpfFormat(form.cpf)) {
      setFormError('Informe um CPF válido no formato 000.000.000-00.');
      return;
    }
    const birthDate = normalizeBirthDate(form.birthDate);
    if (birthDate === null) {
      setFormError('Informe uma data válida no formato DD/MM/AAAA.');
      return;
    }

    savingRef.current = true;
    setSaving(true);
    setFormError('');
    const payload = {
      condominiumId: form.condominiumId,
      unitId: form.unitId || undefined,
      name: form.name.trim(),
      email: form.email.trim(),
      // Sem senha, a API usa o código do condomínio como senha inicial (troca obrigatória no primeiro acesso).
      password: form.password || undefined,
      phone: form.phone.trim(),
      cpf: form.cpf.trim(),
      role: form.role,
      birthDate,
    };

    try {
      const isEditing = Boolean(editingId);
      if (!editingId) {
        await createUser(payload);

        const finishCreate = async () => {
          await loadUsers(true);
          closeModal();
          savingRef.current = false;
          setSaving(false);
        };

        if (Platform.OS === 'web') {
          window.alert('Usuário criado com sucesso.');
          await finishCreate();
          return;
        }

        Alert.alert('Sucesso', 'Usuário criado com sucesso.', [
          {
            text: 'OK',
            onPress: finishCreate,
          },
        ]);
        return;
      }

      await updateUser(editingId, payload);

      const finishUpdate = async () => {
        await loadUsers(true);
        closeModal();
      };

      if (Platform.OS === 'web') {
        window.alert('Usuário atualizado com sucesso.');
        await finishUpdate();
        savingRef.current = false;
        setSaving(false);
        return;
      }

      Alert.alert('Sucesso', 'Usuário atualizado com sucesso.', [
        {
          text: 'OK',
          onPress: finishUpdate,
        },
      ]);
      savingRef.current = false;
      setSaving(false);
    } catch (error) {
      console.error('Erro ao salvar usuário:', error);
      setFormError(getFriendlyErrorMessage(error, 'Não foi possível salvar o usuário.'));
      savingRef.current = false;
      setSaving(false);
    }
  };

  const handleDeactivate = (user: User) => {
    const id = userId(user);
    if (!id) {
      Alert.alert('Erro', 'Não foi possível identificar o usuário.');
      return;
    }
    Alert.alert('Desativar usuário', `Deseja desativar ${user.name}?`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Desativar',
        style: 'destructive',
        onPress: async () => {
          try {
            await deactivateUser(id);
            await loadUsers(true);
            Alert.alert('Sucesso', 'Usuário desativado com sucesso.');
          } catch (error) {
            console.error('Erro ao desativar usuário:', error);
            Alert.alert('Erro', getFriendlyErrorMessage(error, 'Não foi possível desativar o usuário.'));
          }
        },
      },
    ]);
  };

  if (!canManage) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.center}>
          <Ionicons name="lock-closed-outline" size={48} color="#94a3b8" />
          <Text style={styles.deniedTitle}>Acesso negado</Text>
          <Text style={styles.deniedText}>Esta tela é permitida apenas para administrador ou síndico.</Text>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <View>
          <Text style={styles.title}>Usuários</Text>
          <Text style={styles.subtitle}>Moradores, síndicos, porteiros e administradores</Text>
        </View>
        <TouchableOpacity style={styles.addBtn} onPress={openCreate}>
          <Ionicons name="add" size={22} color="#fff" />
          <Text style={styles.addBtnText}>Novo</Text>
        </TouchableOpacity>
      </View>

      {loading && !refreshing ? (
        <ActivityIndicator size="large" color="#176b87" style={styles.loader} />
      ) : listError ? (
        <View style={styles.center}>
          <Ionicons name="alert-circle-outline" size={36} color="#ef4444" />
          <Text style={styles.errorText}>{listError}</Text>
          <TouchableOpacity style={styles.retryBtn} onPress={() => loadUsers()}>
            <Text style={styles.retryText}>Tentar novamente</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={users}
          keyExtractor={(item, index) => userId(item) || `${item.email}-${index}`}
          contentContainerStyle={styles.list}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); loadUsers(true); }} colors={['#176b87']} />}
          ListEmptyComponent={<View style={styles.center}><Text style={styles.emptyText}>Nenhum usuário ativo cadastrado.</Text></View>}
          renderItem={({ item }) => (
            <View style={styles.card}>
              <View style={styles.cardTop}>
                <Text style={styles.cardTitle}>{item.name}</Text>
                <Text style={styles.badge}>{item.role}</Text>
              </View>
              <Text style={styles.cardText}>Login: {item.registrationNumber}</Text>
              <Text style={styles.cardText}>Unidade: {unitLabel(allUnits.get(item.unitId ?? ''))}</Text>
              <Text style={styles.cardText}>E-mail: {item.email}</Text>
              <Text style={styles.cardText}>Telefone: {item.phone}</Text>
              <Text style={styles.cardText}>Ativo: {item.isActive ? 'Sim' : 'Não'}</Text>
              <View style={styles.actions}>
                <TouchableOpacity style={styles.actionBtn} onPress={() => openEdit(item)}>
                  <Ionicons name="create-outline" size={18} color="#2563eb" />
                  <Text style={styles.editText}>Editar</Text>
                </TouchableOpacity>
                <TouchableOpacity style={styles.actionBtn} onPress={() => handleDeactivate(item)}>
                  <Ionicons name="trash-outline" size={18} color="#dc2626" />
                  <Text style={styles.deleteText}>Desativar</Text>
                </TouchableOpacity>
              </View>
            </View>
          )}
        />
      )}

      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>{editingId ? 'Editar Usuário' : 'Novo Usuário'}</Text>
            <TouchableOpacity onPress={closeModal}><Ionicons name="close" size={26} color="#64748b" /></TouchableOpacity>
          </View>
          <ScrollView contentContainerStyle={styles.form} keyboardShouldPersistTaps="handled">
            {formError ? <Text style={styles.formError}>{formError}</Text> : null}

            <Text style={styles.label}>Condomínio *</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false}>
              {condominiums.map(item => <Choice key={item.id} label={item.name} selected={form.condominiumId === item.id} onPress={() => setForm({ ...form, condominiumId: item.id, unitId: '' })} />)}
            </ScrollView>

            <Text style={styles.label}>Unidade</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false}>
              <Choice label="Sem unidade" selected={!form.unitId} onPress={() => setForm({ ...form, unitId: '' })} />
              {units.map(item => <Choice key={item.id} label={unitLabel(item)} selected={form.unitId === item.id} onPress={() => setForm({ ...form, unitId: item.id })} />)}
            </ScrollView>

            <Field label="Nome *" value={form.name} onChangeText={name => setForm({ ...form, name })} />
            <Field label="E-mail *" keyboardType="email-address" autoCapitalize="none" value={form.email} onChangeText={email => setForm({ ...form, email })} />
            <Field label="Senha inicial (opcional; em branco usa o código do condomínio)" secureTextEntry value={form.password} onChangeText={password => setForm({ ...form, password })} />
            <Field label="Telefone *" placeholder="31 9999-2702" keyboardType="phone-pad" maxLength={12} value={form.phone} onChangeText={phone => setForm({ ...form, phone: maskPhoneBR(phone) })} />
            <Field label="CPF *" placeholder="000.000.000-00" keyboardType="numeric" maxLength={14} value={form.cpf} onChangeText={cpf => setForm({ ...form, cpf: maskCpf(cpf) })} />

            <Text style={styles.label}>Perfil *</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false}>
              {ROLES.map(item => <Choice key={item.value} label={item.label} selected={form.role === item.value} onPress={() => setForm({ ...form, role: item.value })} />)}
            </ScrollView>

            <Field label="Nascimento" placeholder="DD/MM/AAAA" keyboardType="numeric" maxLength={10} value={form.birthDate} onChangeText={birthDate => setForm({ ...form, birthDate: maskDateBR(birthDate) })} />
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

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 20, paddingVertical: 16, gap: 12, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0' },
  title: { fontSize: 20, fontWeight: '700', color: '#1e293b' },
  subtitle: { fontSize: 12, color: '#64748b', marginTop: 2, maxWidth: 230 },
  addBtn: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#176b87', borderRadius: 8, paddingHorizontal: 14, paddingVertical: 8, gap: 4 },
  addBtnText: { color: '#fff', fontWeight: '700', fontSize: 14 },
  disabled: { opacity: 0.55 },
  loader: { marginTop: 60 },
  list: { padding: 16, paddingBottom: 80, gap: 12, flexGrow: 1 },
  card: { backgroundColor: '#fff', borderRadius: 14, padding: 16, shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 4, elevation: 2 },
  cardTop: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 8, marginBottom: 6 },
  cardTitle: { fontSize: 16, fontWeight: '700', color: '#1e293b', flex: 1 },
  badge: { color: '#176b87', backgroundColor: '#e0f2fe', borderRadius: 12, paddingHorizontal: 9, paddingVertical: 3, fontSize: 11, fontWeight: '700' },
  cardText: { fontSize: 13, color: '#64748b', marginTop: 3 },
  actions: { flexDirection: 'row', gap: 14, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: '#e2e8f0' },
  actionBtn: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  editText: { color: '#2563eb', fontWeight: '600' },
  deleteText: { color: '#dc2626', fontWeight: '600' },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 },
  deniedTitle: { color: '#1e293b', fontSize: 18, fontWeight: '700', marginTop: 12 },
  deniedText: { color: '#64748b', fontSize: 14, textAlign: 'center', marginTop: 6 },
  errorText: { color: '#64748b', fontSize: 14, textAlign: 'center', marginTop: 8 },
  emptyText: { color: '#94a3b8', fontSize: 15, fontWeight: '600' },
  retryBtn: { marginTop: 16, paddingHorizontal: 20, paddingVertical: 10, backgroundColor: '#176b87', borderRadius: 8 },
  retryText: { color: '#fff', fontWeight: '600' },
  modal: { flex: 1, backgroundColor: '#f8fafc' },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0' },
  modalTitle: { fontSize: 18, fontWeight: '700', color: '#1e293b' },
  form: { padding: 20, paddingBottom: 40 },
  formError: { color: '#dc2626', backgroundColor: '#fef2f2', borderRadius: 8, padding: 12, marginBottom: 8 },
  label: { fontSize: 13, fontWeight: '600', color: '#475569', marginBottom: 6, marginTop: 12 },
  input: { borderWidth: 1, borderColor: '#e2e8f0', borderRadius: 10, padding: 12, fontSize: 15, backgroundColor: '#fff' },
  choice: { borderWidth: 1, borderColor: '#e2e8f0', borderRadius: 20, paddingHorizontal: 14, paddingVertical: 8, marginRight: 8, backgroundColor: '#fff' },
  choiceActive: { backgroundColor: '#176b87', borderColor: '#176b87' },
  choiceText: { color: '#475569', fontWeight: '600' },
  choiceTextActive: { color: '#fff' },
  saveBtn: { backgroundColor: '#176b87', borderRadius: 12, padding: 16, alignItems: 'center', marginTop: 24 },
  saveBtnText: { color: '#fff', fontSize: 16, fontWeight: '700' },
});
