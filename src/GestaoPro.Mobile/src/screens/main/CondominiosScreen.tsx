import React, { useCallback, useRef, useState } from 'react';
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

import { Condominium, createCondominium, deleteCondominium, getCondominiums, getFriendlyErrorMessage, updateCondominium } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { maskCep, maskCnpj, onlyDigits } from '../../utils/formatters';

const INITIAL_FORM = {
  name: '',
  cnpj: '',
  street: '',
  number: '',
  neighborhood: '',
  city: '',
  state: '',
  zipCode: '',
  rules: '',
  regulations: '',
};

function canManageCondominiums(role?: string) {
  return ['admin', 'sindico'].includes((role ?? '').toLowerCase());
}

export default function CondominiosScreen() {
  const { session } = useAuth();
  const canManage = canManageCondominiums(session?.role);
  const [condominiums, setCondominiums] = useState<Condominium[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [listError, setListError] = useState('');
  const [modalVisible, setModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState(INITIAL_FORM);
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const savingRef = useRef(false);

  const loadCondominiums = useCallback(async (silent = false) => {
    if (!canManage) {
      setLoading(false);
      return;
    }

    if (!silent) setLoading(true);
    setListError('');
    try {
      setCondominiums(await getCondominiums());
    } catch (error: any) {
      console.error('Erro ao carregar condomínios:', error);
      setListError(getFriendlyErrorMessage(error, 'Não foi possível carregar os condomínios.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [canManage]);

  useFocusEffect(useCallback(() => { loadCondominiums(); }, [loadCondominiums]));

  const closeModal = () => {
    setModalVisible(false);
    setEditingId(null);
    setForm(INITIAL_FORM);
    setFormError('');
  };

  const handleSave = async () => {
    if (savingRef.current) return;
    if (!canManage) return;

    if (!form.name.trim()) { setFormError('Informe o nome.'); return; }
    if (!form.cnpj.trim()) { setFormError('Informe o CNPJ.'); return; }
    if (!form.street.trim()) { setFormError('Informe a rua.'); return; }
    if (!form.number.trim()) { setFormError('Informe o numero.'); return; }
    if (!form.neighborhood.trim()) { setFormError('Informe o bairro.'); return; }
    if (!form.city.trim()) { setFormError('Informe a cidade.'); return; }
    if (!form.state.trim()) { setFormError('Informe o estado.'); return; }
    if (!form.zipCode.trim()) { setFormError('Informe o CEP.'); return; }
    if (onlyDigits(form.cnpj).length !== 14) {
      setFormError('Informe um CNPJ valido no formato 00.000.000/0000-00.');
      return;
    }
    if (onlyDigits(form.zipCode).length !== 8) {
      setFormError('Informe um CEP valido no formato 00000-000.');
      return;
    }

    savingRef.current = true;
    setSaving(true);
    setFormError('');
    try {
      const payload = {
        name: form.name.trim(),
        cnpj: form.cnpj.trim(),
        street: form.street.trim(),
        number: form.number.trim(),
        neighborhood: form.neighborhood.trim(),
        city: form.city.trim(),
        state: form.state.trim(),
        zipCode: form.zipCode.trim(),
        rules: form.rules.trim(),
        regulations: form.regulations.trim(),
      };

      if (editingId) {
        await updateCondominium(editingId, payload);
      } else {
        await createCondominium(payload);
      }

      const successMessage = editingId ? 'Condominio atualizado com sucesso.' : 'Condominio criado com sucesso.';
      const finishSave = async () => {
        await loadCondominiums(true);
        closeModal();
        savingRef.current = false;
        setSaving(false);
      };

      if (Platform.OS === 'web') {
        window.alert(successMessage);
        await finishSave();
        return;
      }

      Alert.alert('Sucesso', successMessage, [
        { text: 'OK', onPress: finishSave },
      ]);
    } catch (error) {
      console.error('Erro ao salvar condominio:', error);
      setFormError(getFriendlyErrorMessage(error, 'Nao foi possivel salvar o condominio.'));
      savingRef.current = false;
      setSaving(false);
    }
  };
  const handleEdit = (condominium: Condominium) => {
    setEditingId(condominium.id);
    setForm({
      name: condominium.name ?? '',
      cnpj: condominium.cnpj ?? '',
      street: condominium.street ?? '',
      number: condominium.number ?? '',
      neighborhood: condominium.neighborhood ?? '',
      city: condominium.city ?? '',
      state: condominium.state ?? '',
      zipCode: condominium.zipCode ?? '',
      rules: condominium.rules ?? '',
      regulations: condominium.regulations ?? '',
    });
    setFormError('');
    setModalVisible(true);
  };

  const handleDelete = (condominium: Condominium) => {
    Alert.alert('Confirmar remoção', `Deseja realmente remover ${condominium.name}?`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          try {
            await deleteCondominium(condominium.id);
            await loadCondominiums(true);
            Alert.alert('Sucesso', 'Condomínio removido com sucesso.');
          } catch (error) {
            console.error('Erro ao remover condomínio:', error);
            Alert.alert('Erro', getFriendlyErrorMessage(error, 'Não foi possível remover o condomínio.'));
          }
        },
      },
    ]);
  };

  if (!canManage) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.denied}>
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
          <Text style={styles.title}>Condomínios</Text>
          <Text style={styles.subtitle}>Cadastro e consulta de condomínios</Text>
        </View>
        <TouchableOpacity style={styles.addBtn} onPress={() => { closeModal(); setModalVisible(true); }}>
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
          <TouchableOpacity style={styles.retryBtn} onPress={() => loadCondominiums()}>
            <Text style={styles.retryText}>Tentar novamente</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={condominiums}
          keyExtractor={item => item.id}
          contentContainerStyle={styles.list}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => { setRefreshing(true); loadCondominiums(true); }}
              colors={['#176b87']}
            />
          }
          ListEmptyComponent={
            <View style={styles.center}>
              <Ionicons name="business-outline" size={48} color="#cbd5e1" />
              <Text style={styles.emptyText}>Nenhum condomínio cadastrado.</Text>
            </View>
          }
          renderItem={({ item }) => (
            <View style={styles.card}>
              <Text style={styles.cardTitle}>{item.name}</Text>
              <Text style={styles.cardText}>CNPJ: {item.cnpj}</Text>
              <Text style={styles.cardText}>Cidade: {item.city}</Text>
              <Text style={styles.cardText}>UF: {item.state}</Text>
              <Text style={styles.cardText}>Moradores: {item.residentCount}</Text>
              <View style={styles.actionRow}>
                <TouchableOpacity style={styles.actionBtn} onPress={() => handleEdit(item)}>
                  <Ionicons name="create-outline" size={18} color="#2563eb" />
                  <Text style={styles.editText}>Editar</Text>
                </TouchableOpacity>
                <TouchableOpacity style={styles.actionBtn} onPress={() => handleDelete(item)}>
                  <Ionicons name="trash-outline" size={18} color="#dc2626" />
                  <Text style={styles.deleteText}>Remover</Text>
                </TouchableOpacity>
              </View>
            </View>
          )}
        />
      )}

      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>{editingId ? 'Editar Condominio' : 'Novo Condominio'}</Text>
            <TouchableOpacity onPress={closeModal}>
              <Ionicons name="close" size={26} color="#64748b" />
            </TouchableOpacity>
          </View>
          <ScrollView contentContainerStyle={styles.form} keyboardShouldPersistTaps="handled">
            {formError ? <Text style={styles.formError}>{formError}</Text> : null}
            <Field label="Nome *" value={form.name} onChangeText={name => setForm({ ...form, name })} />
            <Field label="CNPJ *" placeholder="00.000.000/0000-00" keyboardType="numeric" maxLength={18} value={form.cnpj} onChangeText={cnpj => setForm({ ...form, cnpj: maskCnpj(cnpj) })} />
            <Field label="Rua *" value={form.street} onChangeText={street => setForm({ ...form, street })} />
            <Field label="Número *" value={form.number} onChangeText={number => setForm({ ...form, number })} />
            <Field label="Bairro *" value={form.neighborhood} onChangeText={neighborhood => setForm({ ...form, neighborhood })} />
            <Field label="Cidade *" value={form.city} onChangeText={city => setForm({ ...form, city })} />
            <Field label="UF *" value={form.state} onChangeText={state => setForm({ ...form, state })} />
            <Field label="CEP *" placeholder="00000-000" keyboardType="numeric" maxLength={9} value={form.zipCode} onChangeText={zipCode => setForm({ ...form, zipCode: maskCep(zipCode) })} />
            <Field label="Regras" multiline value={form.rules} onChangeText={rules => setForm({ ...form, rules })} />
            <Field label="Regulamento" multiline value={form.regulations} onChangeText={regulations => setForm({ ...form, regulations })} />
            <TouchableOpacity style={[styles.saveBtn, saving && styles.disabled]} onPress={handleSave} disabled={saving}>
              <Text style={styles.saveBtnText}>{saving ? 'Salvando...' : editingId ? 'Atualizar' : 'Salvar'}</Text>
            </TouchableOpacity>
          </ScrollView>
        </SafeAreaView>
      </Modal>
    </SafeAreaView>
  );
}

function Field({
  label,
  multiline = false,
  ...inputProps
}: React.ComponentProps<typeof TextInput> & { label: string }) {
  return (
    <View>
      <Text style={styles.label}>{label}</Text>
      <TextInput
        {...inputProps}
        multiline={multiline}
        style={[styles.input, multiline && styles.textarea]}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  header: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingHorizontal: 20, paddingVertical: 16, gap: 12,
    backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0',
  },
  title: { fontSize: 20, fontWeight: '700', color: '#1e293b' },
  subtitle: { fontSize: 12, color: '#64748b', marginTop: 2 },
  addBtn: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#176b87', borderRadius: 8, paddingHorizontal: 14, paddingVertical: 8, gap: 4 },
  addBtnText: { color: '#fff', fontWeight: '700', fontSize: 14 },
  disabled: { opacity: 0.55 },
  loader: { marginTop: 60 },
  list: { padding: 16, paddingBottom: 80, gap: 12, flexGrow: 1 },
  card: { backgroundColor: '#fff', borderRadius: 14, padding: 16, shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 4, elevation: 2 },
  cardTitle: { fontSize: 16, fontWeight: '700', color: '#1e293b', marginBottom: 6 },
  cardText: { fontSize: 13, color: '#64748b', marginTop: 3 },
  actionRow: { flexDirection: 'row', alignItems: 'center', gap: 12, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: '#e2e8f0' },
  actionBtn: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  editText: { color: '#2563eb', fontWeight: '600' },
  deleteBtn: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: '#e2e8f0' },
  deleteText: { color: '#dc2626', fontWeight: '600' },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 },
  emptyText: { color: '#94a3b8', fontSize: 15, marginTop: 12, fontWeight: '600' },
  errorText: { color: '#64748b', fontSize: 14, textAlign: 'center', marginTop: 8 },
  retryBtn: { marginTop: 16, paddingHorizontal: 20, paddingVertical: 10, backgroundColor: '#176b87', borderRadius: 8 },
  retryText: { color: '#fff', fontWeight: '600' },
  denied: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 },
  deniedTitle: { color: '#1e293b', fontSize: 18, fontWeight: '700', marginTop: 12 },
  deniedText: { color: '#64748b', fontSize: 14, textAlign: 'center', marginTop: 6 },
  modal: { flex: 1, backgroundColor: '#f8fafc' },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0' },
  modalTitle: { fontSize: 18, fontWeight: '700', color: '#1e293b' },
  form: { padding: 20, paddingBottom: 40 },
  formError: { color: '#dc2626', backgroundColor: '#fef2f2', borderRadius: 8, padding: 12, marginBottom: 8 },
  label: { fontSize: 13, fontWeight: '600', color: '#475569', marginBottom: 6, marginTop: 12 },
  input: { borderWidth: 1, borderColor: '#e2e8f0', borderRadius: 10, padding: 12, fontSize: 15, backgroundColor: '#fff' },
  textarea: { minHeight: 80, textAlignVertical: 'top' },
  saveBtn: { backgroundColor: '#176b87', borderRadius: 12, padding: 16, alignItems: 'center', marginTop: 24 },
  saveBtnText: { color: '#fff', fontSize: 16, fontWeight: '700' },
});
