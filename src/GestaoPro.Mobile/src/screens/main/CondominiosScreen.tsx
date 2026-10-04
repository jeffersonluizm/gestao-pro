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

import { theme } from '../../theme/theme';
import AppText from '../../components/AppText';
import BackButton from '../../components/BackButton';
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
  defaultPassword: '',
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
    } catch (error) {
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
    if (!editingId && !form.defaultPassword) {
      setFormError('Informe o código do condomínio (mínimo 8 caracteres, sem sequências como 12345678).');
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
        defaultPassword: form.defaultPassword,
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
      defaultPassword: '',
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
          <Ionicons name="lock-closed-outline" size={48} color={theme.color.inkMuted} />
          <AppText style={styles.deniedTitle}>Acesso negado</AppText>
          <AppText style={styles.deniedText}>Esta tela é permitida apenas para administrador ou síndico.</AppText>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <BackButton />
        <View>
          <AppText style={styles.title}>Condomínios</AppText>
          <AppText style={styles.subtitle}>Cadastro e consulta de condomínios</AppText>
        </View>
        <TouchableOpacity accessibilityRole="button" style={styles.addBtn} onPress={() => { closeModal(); setModalVisible(true); }}>
          <Ionicons name="add" size={22} color="#fff" />
          <AppText style={styles.addBtnText}>Novo</AppText>
        </TouchableOpacity>
      </View>

      {loading && !refreshing ? (
        <ActivityIndicator size="large" color={theme.color.action} style={styles.loader} />
      ) : listError ? (
        <View style={styles.center}>
          <Ionicons name="alert-circle-outline" size={36} color={theme.color.danger} />
          <AppText style={styles.errorText}>{listError}</AppText>
          <TouchableOpacity accessibilityRole="button" style={styles.retryBtn} onPress={() => loadCondominiums()}>
            <AppText style={styles.retryText}>Tentar novamente</AppText>
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
              colors={[theme.color.action]}
            />
          }
          ListEmptyComponent={
            <View style={styles.center}>
              <Ionicons name="business-outline" size={48} color={theme.color.borderStrong} />
              <AppText style={styles.emptyText}>Nenhum condomínio cadastrado.</AppText>
            </View>
          }
          renderItem={({ item }) => (
            <View style={styles.card}>
              <AppText style={styles.cardTitle}>{item.name}</AppText>
              <AppText style={styles.cardText}>CNPJ: {item.cnpj}</AppText>
              <AppText style={styles.cardText}>Cidade: {item.city}</AppText>
              <AppText style={styles.cardText}>UF: {item.state}</AppText>
              <AppText style={styles.cardText}>Moradores: {item.residentCount}</AppText>
              <View style={styles.actionRow}>
                <TouchableOpacity accessibilityRole="button" style={styles.actionBtn} onPress={() => handleEdit(item)}>
                  <Ionicons name="create-outline" size={18} color={theme.color.action} />
                  <AppText style={styles.editText}>Editar</AppText>
                </TouchableOpacity>
                <TouchableOpacity accessibilityRole="button" style={styles.actionBtn} onPress={() => handleDelete(item)}>
                  <Ionicons name="trash-outline" size={18} color={theme.color.danger} />
                  <AppText style={styles.deleteText}>Remover</AppText>
                </TouchableOpacity>
              </View>
            </View>
          )}
        />
      )}

      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <AppText style={styles.modalTitle}>{editingId ? 'Editar Condominio' : 'Novo Condominio'}</AppText>
            <TouchableOpacity accessibilityRole="button" onPress={closeModal}>
              <Ionicons name="close" size={26} color={theme.color.inkMuted} />
            </TouchableOpacity>
          </View>
          <ScrollView contentContainerStyle={styles.form} keyboardShouldPersistTaps="handled">
            {formError ? <AppText style={styles.formError}>{formError}</AppText> : null}
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
            <Field
              label={editingId ? 'Código do condomínio (em branco mantém o atual)' : 'Código do condomínio *'}
              placeholder="Mínimo 8 caracteres, sem sequências como 12345678"
              secureTextEntry
              autoCapitalize="none"
              value={form.defaultPassword}
              onChangeText={defaultPassword => setForm({ ...form, defaultPassword })}
            />
            <TouchableOpacity accessibilityRole="button" style={[styles.saveBtn, saving && styles.disabled]} onPress={handleSave} disabled={saving}>
              <AppText style={styles.saveBtnText}>{saving ? 'Salvando...' : editingId ? 'Atualizar' : 'Salvar'}</AppText>
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
      <AppText style={styles.label}>{label}</AppText>
      <TextInput
        accessibilityLabel={label.replace(' *', '')}
        {...inputProps}
        multiline={multiline}
        style={[styles.input, multiline && styles.textarea]}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.color.background },
  header: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingHorizontal: 20, paddingVertical: 16, gap: 12,
    backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: theme.color.border,
  },
  title: { fontSize: 20, fontWeight: '700', color: theme.color.ink },
  subtitle: { fontSize: 12, color: theme.color.inkMuted, marginTop: 2 },
  addBtn: { flexDirection: 'row', alignItems: 'center', backgroundColor: theme.color.action, borderRadius: 8, paddingHorizontal: 14, paddingVertical: 8, gap: 4 },
  addBtnText: { color: '#fff', fontWeight: '700', fontSize: 14 },
  disabled: { opacity: 0.55 },
  loader: { marginTop: 60 },
  list: { padding: 16, paddingBottom: 80, gap: 12, flexGrow: 1 },
  card: { backgroundColor: '#fff', borderRadius: 14, padding: 16, shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 4, elevation: 2 },
  cardTitle: { fontSize: 16, fontWeight: '700', color: theme.color.ink, marginBottom: 6 },
  cardText: { fontSize: 13, color: theme.color.inkMuted, marginTop: 3 },
  actionRow: { flexDirection: 'row', alignItems: 'center', gap: 12, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: theme.color.border },
  actionBtn: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  editText: { color: theme.color.action, fontWeight: '600' },
  deleteBtn: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: theme.color.border },
  deleteText: { color: theme.color.danger, fontWeight: '600' },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 },
  emptyText: { color: theme.color.inkMuted, fontSize: 15, marginTop: 12, fontWeight: '600' },
  errorText: { color: theme.color.inkMuted, fontSize: 14, textAlign: 'center', marginTop: 8 },
  retryBtn: { marginTop: 16, paddingHorizontal: 20, paddingVertical: 10, backgroundColor: theme.color.action, borderRadius: 8 },
  retryText: { color: '#fff', fontWeight: '600' },
  denied: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 },
  deniedTitle: { color: theme.color.ink, fontSize: 18, fontWeight: '700', marginTop: 12 },
  deniedText: { color: theme.color.inkMuted, fontSize: 14, textAlign: 'center', marginTop: 6 },
  modal: { flex: 1, backgroundColor: theme.color.background },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: theme.color.border },
  modalTitle: { fontSize: 18, fontWeight: '700', color: theme.color.ink },
  form: { padding: 20, paddingBottom: 40 },
  formError: { color: theme.color.danger, backgroundColor: theme.color.dangerSoft, borderRadius: 8, padding: 12, marginBottom: 8 },
  label: { fontSize: 13, fontWeight: '600', color: theme.color.inkMuted, marginBottom: 6, marginTop: 12 },
  input: { borderWidth: 1, borderColor: theme.color.border, borderRadius: 10, padding: 12, fontSize: 15, backgroundColor: '#fff' },
  textarea: { minHeight: 80, textAlignVertical: 'top' },
  saveBtn: { backgroundColor: theme.color.action, borderRadius: 12, padding: 16, alignItems: 'center', marginTop: 24 },
  saveBtnText: { color: '#fff', fontSize: 16, fontWeight: '700' },
});
