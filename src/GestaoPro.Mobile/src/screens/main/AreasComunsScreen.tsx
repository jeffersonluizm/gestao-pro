import React, { useCallback, useMemo, useRef, useState } from 'react';
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

import {
  CommonArea,
  Condominium,
  createCommonArea,
  deleteCommonArea,
  getCommonAreas,
  getCondominiums,
  getFriendlyErrorMessage,
  updateCommonArea,
} from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { isValidTime, maskTime } from '../../utils/formatters';

import { theme } from '../../theme/theme';
import AppText from '../../components/AppText';
import BackButton from '../../components/BackButton';
function emptyForm(condominiumId = '') {
  return {
    condominiumId,
    name: '',
    description: '',
    capacity: '',
    openTime: '08:00',
    closeTime: '22:00',
    rules: '',
  };
}

function canManageCommonAreas(role?: string) {
  return ['admin', 'sindico'].includes((role ?? '').toLowerCase());
}

export default function AreasComunsScreen() {
  const { session } = useAuth();
  const canManage = canManageCommonAreas(session?.role);
  const [areas, setAreas] = useState<CommonArea[]>([]);
  const [condominiums, setCondominiums] = useState<Condominium[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [listError, setListError] = useState('');
  const [modalVisible, setModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState(() => emptyForm(session?.condominiumId));
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const savingRef = useRef(false);

  const condominiumNames = useMemo(
    () => new Map(condominiums.map(item => [item.id, item.name])),
    [condominiums],
  );

  const loadAreas = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    setListError('');
    try {
      const [areaData, condominiumData] = await Promise.all([getCommonAreas(), getCondominiums()]);
      setAreas(areaData);
      setCondominiums(condominiumData);
    } catch (error) {
      console.error('Erro ao carregar áreas comuns:', error);
      setListError(getFriendlyErrorMessage(error, 'Não foi possível carregar as áreas comuns.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { loadAreas(); }, [loadAreas]));

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

  const openEdit = (area: CommonArea) => {
    setEditingId(area.id);
    setForm({
      condominiumId: area.condominiumId,
      name: area.name,
      description: area.description ?? '',
      capacity: String(area.capacity),
      openTime: area.openTime,
      closeTime: area.closeTime,
      rules: area.rules ?? '',
    });
    setFormError('');
    setModalVisible(true);
  };

  const handleSave = async () => {
    if (savingRef.current) return;
    if (!canManage) return;
    if (!form.condominiumId.trim()) { setFormError('Selecione o condomínio.'); return; }
    if (!form.name.trim()) { setFormError('Informe o nome.'); return; }
    if (!form.capacity.trim()) { setFormError('Informe a capacidade.'); return; }
    if (!form.openTime.trim()) { setFormError('Informe o horário de abertura.'); return; }
    if (!form.closeTime.trim()) { setFormError('Informe o horário de fechamento.'); return; }
    const capacity = Number(form.capacity);
    if (!Number.isInteger(capacity)) {
      setFormError('Capacidade deve ser um número inteiro.');
      return;
    }
    if (!isValidTime(form.openTime) || !isValidTime(form.closeTime)) {
      setFormError('Informe uma hora vÃ¡lida no formato HH:MM.');
      return;
    }
    if (form.name.trim().length > 100) {
      setFormError('Nome deve ter no máximo 100 caracteres.');
      return;
    }

    savingRef.current = true;
    setSaving(true);
    setFormError('');
    const payload = {
      condominiumId: form.condominiumId,
      name: form.name.trim(),
      description: form.description.trim(),
      capacity,
      openTime: form.openTime.trim(),
      closeTime: form.closeTime.trim(),
      rules: form.rules.trim(),
    };

    try {
      if (!editingId) {
        await createCommonArea(payload);

        const finishCreate = async () => {
          await loadAreas(true);
          closeModal();
          savingRef.current = false;
          setSaving(false);
        };

        if (Platform.OS === 'web') {
          window.alert('Área comum criada com sucesso.');
          await finishCreate();
          return;
        }

        Alert.alert('Sucesso', 'Área comum criada com sucesso.', [
          {
            text: 'OK',
            onPress: finishCreate,
          },
        ]);
        return;
      }

      await updateCommonArea(editingId, payload);

      const finishUpdate = async () => {
        await loadAreas(true);
        closeModal();
      };

      if (Platform.OS === 'web') {
        window.alert('Área comum atualizada com sucesso.');
        await finishUpdate();
        savingRef.current = false;
        setSaving(false);
        return;
      }

      Alert.alert('Sucesso', 'Área comum atualizada com sucesso.', [
        {
          text: 'OK',
          onPress: finishUpdate,
        },
      ]);
      savingRef.current = false;
      setSaving(false);
    } catch (error) {
      console.error('Erro ao salvar área comum:', error);
      setFormError(getFriendlyErrorMessage(error, 'Não foi possível salvar a área comum.'));
      savingRef.current = false;
      setSaving(false);
    }
  };

  const handleDelete = (area: CommonArea) => {
    Alert.alert('Remover área comum', `Deseja remover ${area.name}?`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          try {
            await deleteCommonArea(area.id);
            await loadAreas(true);
            Alert.alert('Sucesso', 'Área comum removida com sucesso.');
          } catch (error) {
            console.error('Erro ao remover área comum:', error);
            Alert.alert('Erro', getFriendlyErrorMessage(error, 'Não foi possível remover a área comum.'));
          }
        },
      },
    ]);
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <BackButton />
        <View>
          <AppText style={styles.title}>Áreas Comuns</AppText>
          <AppText style={styles.subtitle}>Espaços reserváveis do condomínio</AppText>
        </View>
        {canManage ? (
          <TouchableOpacity accessibilityRole="button" style={styles.addBtn} onPress={openCreate}>
            <Ionicons name="add" size={22} color="#fff" />
            <AppText style={styles.addBtnText}>Novo</AppText>
          </TouchableOpacity>
        ) : null}
      </View>

      {loading && !refreshing ? (
        <ActivityIndicator size="large" color={theme.color.action} style={styles.loader} />
      ) : listError ? (
        <View style={styles.center}>
          <Ionicons name="alert-circle-outline" size={36} color={theme.color.danger} />
          <AppText style={styles.errorText}>{listError}</AppText>
          <TouchableOpacity accessibilityRole="button" style={styles.retryBtn} onPress={() => loadAreas()}>
            <AppText style={styles.retryText}>Tentar novamente</AppText>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={areas}
          keyExtractor={item => item.id}
          contentContainerStyle={styles.list}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); loadAreas(true); }} colors={[theme.color.action]} />}
          ListEmptyComponent={<View style={styles.center}><AppText style={styles.emptyText}>Nenhuma área comum cadastrada.</AppText></View>}
          renderItem={({ item }) => (
            <View style={styles.card}>
              <View style={styles.cardTop}>
                <AppText style={styles.cardTitle}>{item.name}</AppText>
                <AppText style={[styles.badge, !item.isActive && styles.badgeInactive]}>{item.isActive ? 'Ativa' : 'Inativa'}</AppText>
              </View>
              <AppText style={styles.cardText}>Capacidade: {item.capacity}</AppText>
              <AppText style={styles.cardText}>Abertura: {item.openTime}</AppText>
              <AppText style={styles.cardText}>Fechamento: {item.closeTime}</AppText>
              <AppText style={styles.cardText}>Condomínio: {condominiumNames.get(item.condominiumId) ?? item.condominiumId}</AppText>
              {canManage ? (
                <View style={styles.actions}>
                  <TouchableOpacity accessibilityRole="button" style={styles.actionBtn} onPress={() => openEdit(item)}>
                    <Ionicons name="create-outline" size={18} color={theme.color.action} />
                    <AppText style={styles.editText}>Editar</AppText>
                  </TouchableOpacity>
                  <TouchableOpacity accessibilityRole="button" style={styles.actionBtn} onPress={() => handleDelete(item)}>
                    <Ionicons name="trash-outline" size={18} color={theme.color.danger} />
                    <AppText style={styles.deleteText}>Remover</AppText>
                  </TouchableOpacity>
                </View>
              ) : null}
            </View>
          )}
        />
      )}

      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <AppText style={styles.modalTitle}>{editingId ? 'Editar Área Comum' : 'Nova Área Comum'}</AppText>
            <TouchableOpacity accessibilityRole="button" onPress={closeModal}><Ionicons name="close" size={26} color={theme.color.inkMuted} /></TouchableOpacity>
          </View>
          <ScrollView contentContainerStyle={styles.form} keyboardShouldPersistTaps="handled">
            {formError ? <AppText style={styles.formError}>{formError}</AppText> : null}
            <AppText style={styles.label}>Condomínio *</AppText>
            <ScrollView horizontal showsHorizontalScrollIndicator={false}>
              {condominiums.map(item => <Choice key={item.id} label={item.name} selected={form.condominiumId === item.id} onPress={() => setForm({ ...form, condominiumId: item.id })} />)}
            </ScrollView>
            <Field label="Nome *" maxLength={100} value={form.name} onChangeText={name => setForm({ ...form, name })} />
            <Field label="Descrição" multiline value={form.description} onChangeText={description => setForm({ ...form, description })} />
            <Field label="Capacidade *" keyboardType="number-pad" value={form.capacity} onChangeText={capacity => setForm({ ...form, capacity })} />
            <Field label="Abertura *" placeholder="14:00" keyboardType="numeric" maxLength={5} value={form.openTime} onChangeText={openTime => setForm({ ...form, openTime: maskTime(openTime) })} />
            <Field label="Fechamento *" placeholder="14:00" keyboardType="numeric" maxLength={5} value={form.closeTime} onChangeText={closeTime => setForm({ ...form, closeTime: maskTime(closeTime) })} />
            <Field label="Regras" multiline value={form.rules} onChangeText={rules => setForm({ ...form, rules })} />
            <TouchableOpacity accessibilityRole="button" style={[styles.saveBtn, saving && styles.disabled]} onPress={handleSave} disabled={saving}>
              <AppText style={styles.saveBtnText}>{saving ? 'Salvando...' : editingId ? 'Atualizar' : 'Salvar'}</AppText>
            </TouchableOpacity>
          </ScrollView>
        </SafeAreaView>
      </Modal>
    </SafeAreaView>
  );
}

function Field({ label, multiline = false, ...props }: React.ComponentProps<typeof TextInput> & { label: string }) {
  return <View><AppText style={styles.label}>{label}</AppText><TextInput accessibilityLabel={label.replace(" *", "")} {...props} multiline={multiline} style={[styles.input, multiline && styles.textarea]} /></View>;
}

function Choice({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
  return <TouchableOpacity accessibilityRole="button" style={[styles.choice, selected && styles.choiceActive]} onPress={onPress}><AppText style={[styles.choiceText, selected && styles.choiceTextActive]}>{label}</AppText></TouchableOpacity>;
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.color.background },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 20, paddingVertical: 16, gap: 12, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: theme.color.border },
  title: { fontSize: 20, fontWeight: '700', color: theme.color.ink },
  subtitle: { fontSize: 12, color: theme.color.inkMuted, marginTop: 2, maxWidth: 230 },
  addBtn: { flexDirection: 'row', alignItems: 'center', backgroundColor: theme.color.action, borderRadius: 8, paddingHorizontal: 14, paddingVertical: 8, gap: 4 },
  addBtnText: { color: '#fff', fontWeight: '700', fontSize: 14 },
  disabled: { opacity: 0.55 },
  loader: { marginTop: 60 },
  list: { padding: 16, paddingBottom: 80, gap: 12, flexGrow: 1 },
  card: { backgroundColor: '#fff', borderRadius: 14, padding: 16, shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 4, elevation: 2 },
  cardTop: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 8, marginBottom: 6 },
  cardTitle: { fontSize: 16, fontWeight: '700', color: theme.color.ink, flex: 1 },
  badge: { color: theme.color.success, backgroundColor: theme.color.successSoft, borderRadius: 12, paddingHorizontal: 9, paddingVertical: 3, fontSize: 11, fontWeight: '700' },
  badgeInactive: { color: theme.color.warning, backgroundColor: theme.color.warningSoft },
  cardText: { fontSize: 13, color: theme.color.inkMuted, marginTop: 3 },
  actions: { flexDirection: 'row', gap: 14, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: theme.color.border },
  actionBtn: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  editText: { color: theme.color.action, fontWeight: '600' },
  deleteText: { color: theme.color.danger, fontWeight: '600' },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 },
  emptyText: { color: theme.color.inkMuted, fontSize: 15, fontWeight: '600' },
  errorText: { color: theme.color.inkMuted, fontSize: 14, textAlign: 'center', marginTop: 8 },
  retryBtn: { marginTop: 16, paddingHorizontal: 20, paddingVertical: 10, backgroundColor: theme.color.action, borderRadius: 8 },
  retryText: { color: '#fff', fontWeight: '600' },
  modal: { flex: 1, backgroundColor: theme.color.background },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: theme.color.border },
  modalTitle: { fontSize: 18, fontWeight: '700', color: theme.color.ink },
  form: { padding: 20, paddingBottom: 40 },
  formError: { color: theme.color.danger, backgroundColor: theme.color.dangerSoft, borderRadius: 8, padding: 12, marginBottom: 8 },
  label: { fontSize: 13, fontWeight: '600', color: theme.color.inkMuted, marginBottom: 6, marginTop: 12 },
  input: { borderWidth: 1, borderColor: theme.color.border, borderRadius: 10, padding: 12, fontSize: 15, backgroundColor: '#fff' },
  textarea: { minHeight: 80, textAlignVertical: 'top' },
  choice: { borderWidth: 1, borderColor: theme.color.border, borderRadius: 20, paddingHorizontal: 14, paddingVertical: 8, marginRight: 8, backgroundColor: '#fff' },
  choiceActive: { backgroundColor: theme.color.action, borderColor: theme.color.action },
  choiceText: { color: theme.color.inkMuted, fontWeight: '600' },
  choiceTextActive: { color: '#fff' },
  saveBtn: { backgroundColor: theme.color.action, borderRadius: 12, padding: 16, alignItems: 'center', marginTop: 24 },
  saveBtnText: { color: '#fff', fontSize: 16, fontWeight: '700' },
});
