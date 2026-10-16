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
  getMyOccurrences, createOccurrence, deleteOccurrence, Occurrence, getFriendlyErrorMessage,
} from '../../api/client';
import { useAuth } from '../../context/AuthContext';

import { theme } from '../../theme/theme';
import AppText from '../../components/AppText';
// API enum values (must match C# enum names exactly)
const STATUS_COLOR: Record<string, string> = {
  Open: theme.color.warning,
  InProgress: theme.color.action,
  Concluded: theme.color.success,
};
const STATUS_LABEL: Record<string, string> = {
  Open: 'Aberta',
  InProgress: 'Em andamento',
  Concluded: 'Concluída',
};
const PRIORITY_COLOR: Record<string, string> = {
  Low: theme.color.success,
  Medium: theme.color.warning,
  High: theme.color.danger,
  Urgent: theme.color.danger,
};
const PRIORITY_LABEL: Record<string, string> = {
  Low: 'Baixa',
  Medium: 'Média',
  High: 'Alta',
  Urgent: 'Urgente',
};
const CATEGORY_LABEL: Record<string, string> = {
  Maintenance: 'Manutenção',
  Complaint: 'Reclamação',
  Suggestion: 'Sugestão',
};

// Display options → API enum value
const CATEGORIES: { label: string; value: string }[] = [
  { label: 'Manutenção',  value: 'Maintenance' },
  { label: 'Reclamação',  value: 'Complaint' },
  { label: 'Sugestão',    value: 'Suggestion' },
];
const PRIORITIES: { label: string; value: string }[] = [
  { label: 'Baixa',   value: 'Low' },
  { label: 'Média',   value: 'Medium' },
  { label: 'Alta',    value: 'High' },
  { label: 'Urgente', value: 'Urgent' },
];

export default function OccurrencesScreen() {
  const { session } = useAuth();
  const [occurrences, setOccurrences] = useState<Occurrence[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [listError, setListError] = useState('');
  const [modalVisible, setModalVisible] = useState(false);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState('Maintenance');
  const [priority, setPriority] = useState('Medium');

  async function load(silent = false) {
    if (!silent) setLoading(true);
    setListError('');
    try {
      const data = await getMyOccurrences(session!.userId);
      setOccurrences(data.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()));
    } catch (e) {
      console.error('Erro ao carregar ocorrências:', e);
      setListError(getFriendlyErrorMessage(e, 'Não foi possível carregar as ocorrências.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }

  // Recarrega ao voltar para a aba, sempre com a versão atual de load (sessão em dia).
  const loadRef = useRef(load);
  loadRef.current = load;
  useFocusEffect(useCallback(() => { loadRef.current(); }, []));

  function resetForm() {
    setTitle('');
    setDescription('');
    setCategory('Maintenance');
    setPriority('Medium');
    setFormError('');
  }

  async function handleCreate() {
    if (saving) return;
    setFormError('');
    if (!title.trim()) { setFormError('Informe o título da ocorrência.'); return; }
    if (!description.trim()) { setFormError('Descreva a ocorrência.'); return; }
    setSaving(true);
    try {
      await createOccurrence({
        title: title.trim(),
        description: description.trim(),
        category,
        priority,
        userId: session!.userId,
        condominiumId: session!.condominiumId,
      });
      const finishCreate = async () => {
        await load(true);
        setModalVisible(false);
        resetForm();
      };

      if (Platform.OS === 'web') {
        window.alert('Ocorrência criada com sucesso.');
        await finishCreate();
        return;
      }

      Alert.alert('Sucesso', 'Ocorrência criada com sucesso.', [
        {
          text: 'OK',
          onPress: finishCreate,
        },
      ]);
    } catch (e) {
      console.error('Erro ao criar ocorrência:', e);
      setFormError(getFriendlyErrorMessage(e, 'Não foi possível salvar a ocorrência.'));
    } finally {
      setSaving(false);
    }
  }

  function handleDelete(occurrence: Occurrence) {
    Alert.alert('Confirmar remoção', 'Deseja realmente remover esta ocorrência?', [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          try {
            await deleteOccurrence(occurrence.id);
            await load(true);
            Alert.alert('Sucesso', 'Ocorrência removida com sucesso.');
          } catch (e) {
            console.error('Erro ao remover ocorrência:', e);
            Alert.alert('Erro', getFriendlyErrorMessage(e, 'Não foi possível remover a ocorrência.'));
          }
        },
      },
    ]);
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <AppText style={styles.title}>Ocorrências</AppText>
        <TouchableOpacity accessibilityRole="button" style={styles.addBtn} onPress={() => { resetForm(); setModalVisible(true); }}>
          <Ionicons name="add" size={22} color="#fff" />
          <AppText style={styles.addBtnText}>Nova</AppText>
        </TouchableOpacity>
      </View>

      {loading ? (
        <ActivityIndicator size="large" color={theme.color.action} style={{ marginTop: 60 }} />
      ) : listError ? (
        <View style={styles.errorBox}>
          <Ionicons name="alert-circle-outline" size={32} color={theme.color.danger} />
          <AppText style={styles.errorText}>{listError}</AppText>
          <TouchableOpacity accessibilityRole="button" style={styles.retryBtn} onPress={() => load()}>
            <AppText style={styles.retryText}>Tentar novamente</AppText>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={occurrences}
          keyExtractor={item => item.id}
          contentContainerStyle={styles.list}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => { setRefreshing(true); load(true); }}
              colors={[theme.color.action]}
            />
          }
          renderItem={({ item }) => {
            const sColor = STATUS_COLOR[item.status] ?? theme.color.inkMuted;
            const pColor = PRIORITY_COLOR[item.priority] ?? theme.color.inkMuted;
            const date = item.createdAt ? new Date(item.createdAt).toLocaleDateString('pt-BR') : '';
            return (
              <View style={styles.card}>
                <View style={styles.cardRow}>
                  <AppText style={styles.cardTitle} numberOfLines={1}>{item.title}</AppText>
                  <View style={[styles.badge, { backgroundColor: sColor + '20' }]}>
                    <AppText style={[styles.badgeText, { color: sColor }]}>
                      {STATUS_LABEL[item.status] ?? item.status}
                    </AppText>
                  </View>
                </View>
                <View style={styles.cardMeta}>
                  <View style={[styles.priorityDot, { backgroundColor: pColor }]} />
                  <AppText style={styles.cardMetaText}>
                    {PRIORITY_LABEL[item.priority] ?? item.priority} · {CATEGORY_LABEL[item.category] ?? item.category} · {date}
                  </AppText>
                </View>
                {item.description ? (
                  <AppText style={styles.cardDesc} numberOfLines={2}>{item.description}</AppText>
                ) : null}
                <TouchableOpacity accessibilityRole="button" style={styles.deleteBtn} onPress={() => handleDelete(item)}>
                  <Ionicons name="trash-outline" size={18} color={theme.color.danger} />
                  <AppText style={styles.deleteText}>Remover</AppText>
                </TouchableOpacity>
              </View>
            );
          }}
          ListEmptyComponent={
            <View style={styles.empty}>
              <Ionicons name="construct-outline" size={48} color={theme.color.borderStrong} />
              <AppText style={styles.emptyText}>Nenhuma ocorrência registrada.</AppText>
            </View>
          }
        />
      )}

      {/* Modal Nova Ocorrência */}
      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <AppText style={styles.modalTitle}>Nova Ocorrência</AppText>
            <TouchableOpacity accessibilityRole="button" onPress={() => { setModalVisible(false); resetForm(); }}>
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

            <AppText style={styles.label}>Título *</AppText>
            <TextInput accessibilityLabel="Título"
              style={styles.input}
              placeholder="Descreva brevemente o problema..."
              value={title}
              onChangeText={setTitle}
            />

            <AppText style={styles.label}>Categoria</AppText>
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 12 }}>
              {CATEGORIES.map(cat => (
                <TouchableOpacity accessibilityRole="button"
                  key={cat.value}
                  style={[styles.chip, category === cat.value && styles.chipActive]}
                  onPress={() => setCategory(cat.value)}
                >
                  <AppText style={[styles.chipText, category === cat.value && { color: '#fff' }]}>{cat.label}</AppText>
                </TouchableOpacity>
              ))}
            </ScrollView>

            <AppText style={styles.label}>Prioridade</AppText>
            <View style={styles.priorityRow}>
              {PRIORITIES.map(p => (
                <TouchableOpacity accessibilityRole="button"
                  key={p.value}
                  style={[
                    styles.priorityChip,
                    priority === p.value && { backgroundColor: PRIORITY_COLOR[p.value], borderColor: PRIORITY_COLOR[p.value] },
                  ]}
                  onPress={() => setPriority(p.value)}
                >
                  <AppText style={[styles.priorityChipText, priority === p.value && { color: '#fff' }]}>
                    {p.label}
                  </AppText>
                </TouchableOpacity>
              ))}
            </View>

            <AppText style={styles.label}>Descrição *</AppText>
            <TextInput accessibilityLabel="Descrição"
              style={[styles.input, { height: 100, textAlignVertical: 'top' }]}
              placeholder="Descreva a ocorrência com detalhes..."
              multiline
              value={description}
              onChangeText={setDescription}
            />

            <TouchableOpacity accessibilityRole="button" style={styles.saveBtn} onPress={handleCreate} disabled={saving}>
              {saving
                ? <ActivityIndicator color="#fff" />
                : <AppText style={styles.saveBtnText}>Registrar ocorrência</AppText>
              }
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
  addBtn: {
    flexDirection: 'row', alignItems: 'center', backgroundColor: theme.color.action,
    borderRadius: 8, paddingHorizontal: 14, paddingVertical: 8, gap: 4,
  },
  addBtnText: { color: '#fff', fontWeight: '700', fontSize: 14 },
  list: { padding: 16, gap: 12 },
  card: {
    backgroundColor: '#fff', borderRadius: 14, padding: 16,
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06, shadowRadius: 4, elevation: 2,
  },
  cardRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6 },
  cardTitle: { fontSize: 15, fontWeight: '700', color: theme.color.ink, flex: 1, marginRight: 8 },
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 2 },
  badgeText: { fontSize: 11, fontWeight: '700' },
  cardMeta: { flexDirection: 'row', alignItems: 'center', marginBottom: 6 },
  priorityDot: { width: 8, height: 8, borderRadius: 4, marginRight: 6 },
  cardMetaText: { fontSize: 12, color: theme.color.inkMuted },
  cardDesc: { fontSize: 13, color: theme.color.inkMuted, lineHeight: 18 },
  deleteBtn: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: theme.color.border },
  deleteText: { color: theme.color.danger, fontWeight: '600' },
  empty: { alignItems: 'center', paddingTop: 80 },
  emptyText: { color: theme.color.inkMuted, fontSize: 15, marginTop: 12 },
  errorBox: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 24 },
  errorText: { color: theme.color.inkMuted, fontSize: 14, textAlign: 'center', marginTop: 8 },
  retryBtn: { marginTop: 16, paddingHorizontal: 20, paddingVertical: 10, backgroundColor: theme.color.action, borderRadius: 8 },
  retryText: { color: '#fff', fontWeight: '600' },
  modal: { flex: 1, backgroundColor: theme.color.background },
  modalHeader: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    padding: 20, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: theme.color.border,
  },
  modalTitle: { fontSize: 18, fontWeight: '700', color: theme.color.ink },
  modalBody: { padding: 20 },
  formError: {
    flexDirection: 'row', alignItems: 'center', gap: 6,
    backgroundColor: theme.color.dangerSoft, borderRadius: 8, padding: 12, marginBottom: 8,
  },
  formErrorText: { color: theme.color.danger, fontSize: 13, flex: 1 },
  label: { fontSize: 13, fontWeight: '600', color: theme.color.inkMuted, marginBottom: 6, marginTop: 12 },
  input: {
    borderWidth: 1, borderColor: theme.color.border, borderRadius: 10,
    padding: 12, fontSize: 15, backgroundColor: '#fff', marginBottom: 4,
  },
  chip: {
    borderWidth: 1, borderColor: theme.color.border, borderRadius: 20,
    paddingHorizontal: 14, paddingVertical: 8, marginRight: 8, backgroundColor: '#fff',
  },
  chipActive: { backgroundColor: theme.color.action, borderColor: theme.color.action },
  chipText: { fontSize: 14, color: theme.color.inkMuted, fontWeight: '600' },
  priorityRow: { flexDirection: 'row', gap: 8, marginBottom: 4 },
  priorityChip: {
    borderWidth: 1, borderColor: theme.color.border, borderRadius: 8,
    paddingHorizontal: 12, paddingVertical: 8, backgroundColor: '#fff',
  },
  priorityChipText: { fontSize: 13, color: theme.color.inkMuted, fontWeight: '600' },
  saveBtn: {
    backgroundColor: theme.color.action, borderRadius: 12, padding: 16,
    alignItems: 'center', marginTop: 24, marginBottom: 40,
  },
  saveBtnText: { color: '#fff', fontSize: 16, fontWeight: '700' },
});
