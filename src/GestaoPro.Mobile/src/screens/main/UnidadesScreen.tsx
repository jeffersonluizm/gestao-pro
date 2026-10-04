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

import {
  Condominium,
  createUnit,
  deleteUnit,
  getCondominiums,
  getFriendlyErrorMessage,
  getUnitsByCondominium,
  Unit,
  updateUnit,
} from '../../api/client';
import { useAuth } from '../../context/AuthContext';

import { theme } from '../../theme/theme';
import AppText from '../../components/AppText';
import BackButton from '../../components/BackButton';
const TYPES = [
  { value: 'apt', label: 'Apartamento' },
  { value: 'house', label: 'Casa' },
];

function emptyForm(condominiumId = '') {
  return {
    condominiumId,
    number: '',
    block: '',
    floor: '',
    type: 'apt',
  };
}

function canManageUnits(role?: string) {
  return ['admin', 'sindico'].includes((role ?? '').toLowerCase());
}

export default function UnidadesScreen() {
  const { session } = useAuth();
  const canManage = canManageUnits(session?.role);
  const [condominiums, setCondominiums] = useState<Condominium[]>([]);
  const [selectedCondominiumId, setSelectedCondominiumId] = useState('');
  const [units, setUnits] = useState<Unit[]>([]);
  const [loadingCondominiums, setLoadingCondominiums] = useState(true);
  const [loadingUnits, setLoadingUnits] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [listError, setListError] = useState('');
  const [modalVisible, setModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm());
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const savingRef = useRef(false);

  const loadCondominiums = useCallback(async () => {
    if (!canManage) {
      setLoadingCondominiums(false);
      return;
    }
    setLoadingCondominiums(true);
    setListError('');
    try {
      setCondominiums(await getCondominiums());
    } catch (error) {
      console.error('Erro ao carregar condomínios:', error);
      setListError(getFriendlyErrorMessage(error, 'Não foi possível carregar os condomínios.'));
    } finally {
      setLoadingCondominiums(false);
    }
  }, [canManage]);

  const loadUnits = useCallback(async (condominiumId = selectedCondominiumId, silent = false) => {
    if (!condominiumId) {
      setUnits([]);
      setRefreshing(false);
      return;
    }
    if (!silent) setLoadingUnits(true);
    setListError('');
    try {
      setUnits(await getUnitsByCondominium(condominiumId));
    } catch (error) {
      console.error('Erro ao carregar unidades:', error);
      setListError(getFriendlyErrorMessage(error, 'Não foi possível carregar as unidades.'));
    } finally {
      setLoadingUnits(false);
      setRefreshing(false);
    }
  }, [selectedCondominiumId]);

  useFocusEffect(useCallback(() => { loadCondominiums(); }, [loadCondominiums]));

  const selectCondominium = (id: string) => {
    setSelectedCondominiumId(id);
    setUnits([]);
    loadUnits(id);
  };

  const closeModal = () => {
    setModalVisible(false);
    setEditingId(null);
    setForm(emptyForm(selectedCondominiumId));
    setFormError('');
  };

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm(selectedCondominiumId));
    setFormError('');
    setModalVisible(true);
  };

  const openEdit = (unit: Unit) => {
    setEditingId(unit.id);
    setForm({
      condominiumId: unit.condominiumId,
      number: unit.number,
      block: unit.block ?? '',
      floor: unit.floor === undefined || unit.floor === null ? '' : String(unit.floor),
      type: unit.type,
    });
    setFormError('');
    setModalVisible(true);
  };

  const handleSave = async () => {
    if (savingRef.current) return;

    if (!form.condominiumId) { setFormError('Selecione o condomínio.'); return; }
    if (!form.number.trim()) { setFormError('Informe o número da unidade.'); return; }
    if (!form.type) { setFormError('Selecione o tipo da unidade.'); return; }
    const floor = form.floor.trim() === '' ? undefined : Number(form.floor);
    if (floor !== undefined && !Number.isInteger(floor)) {
      setFormError('Andar deve ser um número inteiro.');
      return;
    }

    savingRef.current = true;
    setSaving(true);
    setFormError('');
    const payload = {
      condominiumId: form.condominiumId,
      number: form.number.trim(),
      block: form.block.trim() || undefined,
      floor,
      type: form.type,
    };

    try {
      if (!editingId) {
        await createUnit(payload);

        const finishCreate = async () => {
          await loadUnits(selectedCondominiumId, true);
          closeModal();
          savingRef.current = false;
          setSaving(false);
        };

        if (Platform.OS === 'web') {
          window.alert('Unidade criada com sucesso.');
          await finishCreate();
          return;
        }

        Alert.alert('Sucesso', 'Unidade criada com sucesso.', [
          {
            text: 'OK',
            onPress: finishCreate,
          },
        ]);
        return;
      }

      await updateUnit(editingId, payload);

      const finishUpdate = async () => {
        await loadUnits(selectedCondominiumId, true);
        closeModal();
      };

      if (Platform.OS === 'web') {
        window.alert('Unidade atualizada com sucesso.');
        await finishUpdate();
        savingRef.current = false;
        setSaving(false);
        return;
      }

      Alert.alert('Sucesso', 'Unidade atualizada com sucesso.', [
        {
          text: 'OK',
          onPress: finishUpdate,
        },
      ]);
      savingRef.current = false;
      setSaving(false);
    } catch (error) {
      console.error('Erro ao salvar unidade:', error);
      const message = getFriendlyErrorMessage(error, 'Não foi possível salvar a unidade.');
      setFormError(message.toLowerCase().includes('exist') || message.toLowerCase().includes('duplic')
        ? 'Já existe uma unidade com os dados informados.'
        : message);
      savingRef.current = false;
      setSaving(false);
    }
  };

  const handleDelete = (unit: Unit) => {
    Alert.alert('Confirmar remoção', `Deseja realmente remover a unidade ${unit.number}?`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          try {
            await deleteUnit(unit.id);
            await loadUnits(selectedCondominiumId, true);
            Alert.alert('Sucesso', 'Unidade removida com sucesso.');
          } catch (error) {
            console.error('Erro ao remover unidade:', error);
            Alert.alert('Erro', getFriendlyErrorMessage(error, 'Não foi possível remover a unidade.'));
          }
        },
      },
    ]);
  };

  if (!canManage) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.center}>
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
          <AppText style={styles.title}>Unidades</AppText>
          <AppText style={styles.subtitle}>Apartamentos, casas e vínculos de moradores</AppText>
        </View>
        <TouchableOpacity accessibilityRole="button" style={[styles.addBtn, !selectedCondominiumId && styles.disabled]} onPress={openCreate} disabled={!selectedCondominiumId}>
          <Ionicons name="add" size={22} color="#fff" />
          <AppText style={styles.addBtnText}>Novo</AppText>
        </TouchableOpacity>
      </View>

      <View style={styles.filter}>
        <AppText style={styles.label}>Condomínio</AppText>
        {loadingCondominiums ? (
          <ActivityIndicator color={theme.color.action} />
        ) : (
          <ScrollView horizontal showsHorizontalScrollIndicator={false}>
            {condominiums.map(item => (
              <Choice key={item.id} label={item.name} selected={selectedCondominiumId === item.id} onPress={() => selectCondominium(item.id)} />
            ))}
          </ScrollView>
        )}
      </View>

      {loadingUnits && !refreshing ? (
        <ActivityIndicator size="large" color={theme.color.action} style={styles.loader} />
      ) : listError ? (
        <View style={styles.center}>
          <Ionicons name="alert-circle-outline" size={36} color={theme.color.danger} />
          <AppText style={styles.errorText}>{listError}</AppText>
          <TouchableOpacity accessibilityRole="button" style={styles.retryBtn} onPress={() => selectedCondominiumId ? loadUnits() : loadCondominiums()}>
            <AppText style={styles.retryText}>Tentar novamente</AppText>
          </TouchableOpacity>
        </View>
      ) : !selectedCondominiumId ? (
        <View style={styles.center}>
          <Ionicons name="business-outline" size={48} color={theme.color.borderStrong} />
          <AppText style={styles.emptyText}>Selecione um condomínio para consultar as unidades.</AppText>
        </View>
      ) : (
        <FlatList
          data={units}
          keyExtractor={item => item.id}
          contentContainerStyle={styles.list}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); loadUnits(selectedCondominiumId, true); }} colors={[theme.color.action]} />}
          ListEmptyComponent={<View style={styles.center}><AppText style={styles.emptyText}>Nenhuma unidade cadastrada.</AppText></View>}
          renderItem={({ item }) => (
            <View style={styles.card}>
              <View style={styles.cardTop}>
                <AppText style={styles.cardTitle}>Unidade {item.number}</AppText>
                <AppText style={styles.badge}>{item.type}</AppText>
              </View>
              <AppText style={styles.cardText}>Bloco: {item.block || '-'}</AppText>
              <AppText style={styles.cardText}>Andar: {item.floor ?? '-'}</AppText>
              <AppText style={styles.cardText}>Tipo: {item.type}</AppText>
              <AppText style={styles.cardText}>Moradores: {item.residentCount}</AppText>
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
            </View>
          )}
        />
      )}

      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <AppText style={styles.modalTitle}>{editingId ? 'Editar Unidade' : 'Nova Unidade'}</AppText>
            <TouchableOpacity accessibilityRole="button" onPress={closeModal}><Ionicons name="close" size={26} color={theme.color.inkMuted} /></TouchableOpacity>
          </View>
          <ScrollView contentContainerStyle={styles.form} keyboardShouldPersistTaps="handled">
            {formError ? <AppText style={styles.formError}>{formError}</AppText> : null}
            <AppText style={styles.label}>Condomínio *</AppText>
            <ScrollView horizontal showsHorizontalScrollIndicator={false}>
              {condominiums.map(item => <Choice key={item.id} label={item.name} selected={form.condominiumId === item.id} onPress={() => setForm({ ...form, condominiumId: item.id })} />)}
            </ScrollView>
            <Field label="Número *" value={form.number} onChangeText={number => setForm({ ...form, number })} />
            <Field label="Bloco" value={form.block} onChangeText={block => setForm({ ...form, block })} />
            <Field label="Andar" keyboardType="number-pad" value={form.floor} onChangeText={floor => setForm({ ...form, floor })} />
            <AppText style={styles.label}>Tipo *</AppText>
            <ScrollView horizontal showsHorizontalScrollIndicator={false}>
              {TYPES.map(item => <Choice key={item.value} label={item.label} selected={form.type === item.value} onPress={() => setForm({ ...form, type: item.value })} />)}
            </ScrollView>
            <TouchableOpacity accessibilityRole="button" style={[styles.saveBtn, saving && styles.disabled]} onPress={handleSave} disabled={saving}>
              <AppText style={styles.saveBtnText}>{saving ? 'Salvando...' : editingId ? 'Atualizar' : 'Salvar'}</AppText>
            </TouchableOpacity>
          </ScrollView>
        </SafeAreaView>
      </Modal>
    </SafeAreaView>
  );
}

function Field({ label, ...props }: React.ComponentProps<typeof TextInput> & { label: string }) {
  return <View><AppText style={styles.label}>{label}</AppText><TextInput accessibilityLabel={label.replace(" *", "")} {...props} style={styles.input} /></View>;
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
  disabled: { opacity: 0.45 },
  filter: { backgroundColor: '#fff', paddingHorizontal: 16, paddingBottom: 14, borderBottomWidth: 1, borderBottomColor: theme.color.border },
  loader: { marginTop: 60 },
  list: { padding: 16, paddingBottom: 80, gap: 12, flexGrow: 1 },
  card: { backgroundColor: '#fff', borderRadius: 14, padding: 16, shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 4, elevation: 2 },
  cardTop: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: 8, marginBottom: 6 },
  cardTitle: { fontSize: 16, fontWeight: '700', color: theme.color.ink, flex: 1 },
  badge: { color: theme.color.action, backgroundColor: theme.color.actionSoft, borderRadius: 12, paddingHorizontal: 9, paddingVertical: 3, fontSize: 11, fontWeight: '700' },
  cardText: { fontSize: 13, color: theme.color.inkMuted, marginTop: 3 },
  actions: { flexDirection: 'row', alignItems: 'center', gap: 16, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: theme.color.border },
  actionBtn: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  editText: { color: theme.color.action, fontWeight: '600' },
  deleteText: { color: theme.color.danger, fontWeight: '600' },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 },
  deniedTitle: { color: theme.color.ink, fontSize: 18, fontWeight: '700', marginTop: 12 },
  deniedText: { color: theme.color.inkMuted, fontSize: 14, textAlign: 'center', marginTop: 6 },
  emptyText: { color: theme.color.inkMuted, fontSize: 15, fontWeight: '600', textAlign: 'center' },
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
  choice: { borderWidth: 1, borderColor: theme.color.border, borderRadius: 20, paddingHorizontal: 14, paddingVertical: 8, marginRight: 8, backgroundColor: '#fff' },
  choiceActive: { backgroundColor: theme.color.action, borderColor: theme.color.action },
  choiceText: { color: theme.color.inkMuted, fontWeight: '600' },
  choiceTextActive: { color: '#fff' },
  saveBtn: { backgroundColor: theme.color.action, borderRadius: 12, padding: 16, alignItems: 'center', marginTop: 24 },
  saveBtnText: { color: '#fff', fontSize: 16, fontWeight: '700' },
});
