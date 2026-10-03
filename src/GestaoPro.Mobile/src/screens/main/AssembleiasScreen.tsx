// src/screens/main/AssembleiasScreen.tsx
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TextInput, TouchableOpacity,
  Modal, Alert, ActivityIndicator, Platform, RefreshControl, ScrollView,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';

import { useAuth } from '../../context/AuthContext';
import {
  Assembly,
  Condominium,
  createAssembly,
  deleteAssembly,
  getAssemblies,
  getAssembliesByCondominium,
  getCondominiums,
  getFriendlyErrorMessage,
  updateAssembly,
} from '../../api/client';
import { sw, sf, HP } from '../../utils/responsive';
import { apiDateToBR, dateBRToIso, maskDateBR } from '../../utils/formatters';

type AssemblyStatus = 'agendada' | 'em_andamento' | 'encerrada' | 'cancelada';

const STATUS_OPTIONS: { value: AssemblyStatus; label: string }[] = [
  { value: 'agendada', label: 'Agendada' },
  { value: 'em_andamento', label: 'Em andamento' },
  { value: 'encerrada', label: 'Encerrada' },
  { value: 'cancelada', label: 'Cancelada' },
];

function canManageAssemblies(role?: string) {
  return ['admin', 'sindico'].includes((role ?? '').toLowerCase());
}

function emptyForm(condominiumId = '') {
  return {
    condominiumId,
    title: '',
    description: '',
    startDate: '',
    endDate: '',
    status: 'agendada' as AssemblyStatus,
    topicsText: '',
    minutes: '',
  };
}

function formatDateForInput(value?: string) {
  return apiDateToBR(value);
}

function formatDate(value?: string) {
  if (!value) return '';
  const date = new Date(value);
  return Number.isFinite(date.getTime())
    ? date.toLocaleDateString('pt-BR')
    : value;
}

function normalizeDateForApi(value: string, endOfDay = false) {
  return dateBRToIso(value, endOfDay);
}

function statusInfo(status: string) {
  if (status === 'em_andamento' || status === 'en_andamento') {
    return { bg: '#dcfce7', text: '#15803d', label: 'Em andamento' };
  }
  if (status === 'encerrada') {
    return { bg: '#e0f2fe', text: '#0369a1', label: 'Encerrada' };
  }
  if (status === 'cancelada') {
    return { bg: '#fee2e2', text: '#dc2626', label: 'Cancelada' };
  }
  return { bg: '#fef3c7', text: '#b45309', label: 'Agendada' };
}

export default function AssembleiasScreen() {
  const { session } = useAuth();
  const canManage = canManageAssemblies(session?.role);

  const [assemblies, setAssemblies] = useState<Assembly[]>([]);
  const [condominiums, setCondominiums] = useState<Condominium[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [listError, setListError] = useState('');
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const savingRef = useRef(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [showHistory, setShowHistory] = useState(false);
  const [filterCondominiumId, setFilterCondominiumId] = useState(session?.condominiumId ?? '');
  const [form, setForm] = useState(() => emptyForm(session?.condominiumId));

  const visibleCondominiums = useMemo(() => {
    if (canManage) return condominiums;
    return condominiums.filter(c => c.id === session?.condominiumId);
  }, [canManage, condominiums, session?.condominiumId]);

  const condominiumName = (id: string) =>
    condominiums.find(c => c.id === id)?.name ?? id;

  const loadAssemblies = useCallback(async (silent = false) => {
    if (!session?.condominiumId) return;
    if (!silent) setLoading(true);
    setListError('');

    try {
      const condos = await getCondominiums();
      setCondominiums(condos);

      const condominiumId = canManage ? filterCondominiumId : session.condominiumId;
      const data = condominiumId
        ? await getAssembliesByCondominium(condominiumId, showHistory)
        : await getAssemblies(showHistory);

      setAssemblies(data.sort((a, b) => new Date(b.startTime).getTime() - new Date(a.startTime).getTime()));
    } catch (error: any) {
      console.error('Erro ao carregar assembleias:', error);
      setListError(getFriendlyErrorMessage(error, 'Não foi possível carregar as assembleias.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [canManage, filterCondominiumId, session?.condominiumId, showHistory]);

  useFocusEffect(useCallback(() => { loadAssemblies(); }, [loadAssemblies]));
  useEffect(() => { loadAssemblies(); }, [loadAssemblies]);

  const onRefresh = () => {
    setRefreshing(true);
    loadAssemblies(true);
  };

  const resetForm = () => {
    setEditingId(null);
    setForm(emptyForm(session?.condominiumId));
    setFormError('');
  };

  const handleSave = async () => {
    if (savingRef.current) return;
    if (!canManage) return;
    setFormError('');

    const condominiumId = form.condominiumId || session?.condominiumId || '';
    if (!condominiumId) { setFormError('Selecione o condomínio.'); return; }
    if (!form.title.trim()) { setFormError('Informe o título.'); return; }
    if (!form.startDate.trim()) { setFormError('Informe a data inicial no formato DD/MM/AAAA.'); return; }

    const startDate = normalizeDateForApi(form.startDate);
    const endDate = normalizeDateForApi(form.endDate || form.startDate, true);
    if (!startDate) {
      setFormError('Informe uma data válida no formato DD/MM/AAAA.');
      return;
    }
    if (!endDate) {
      setFormError('Informe uma data válida no formato DD/MM/AAAA.');
      return;
    }
    if (endDate < startDate) {
      setFormError('A data final deve ser igual ou posterior a data inicial.');
      return;
    }

    savingRef.current = true;
    setSaving(true);
    try {
      const isEditing = Boolean(editingId);
      const payload = {
        title: form.title.trim(),
        description: form.description.trim(),
        scheduledDate: startDate,
        startDate,
        endDate,
        status: form.status,
        topicsText: form.topicsText.trim(),
        minutes: form.minutes.trim(),
      };

      if (editingId) {
        await updateAssembly(editingId, payload);
      } else {
        await createAssembly({
          condominiumId,
          createdBy: session?.userId,
          ...payload,
        });

        const finishCreate = async () => {
          await loadAssemblies(true);
          setModalVisible(false);
          resetForm();
          savingRef.current = false;
          setSaving(false);
        };

        if (Platform.OS === 'web') {
          window.alert('Assembleia criada com sucesso.');
          await finishCreate();
          return;
        }

        Alert.alert('Sucesso', 'Assembleia criada com sucesso.', [
          {
            text: 'OK',
            onPress: finishCreate,
          },
        ]);
        return;
      }

      const finishUpdate = async () => {
        await loadAssemblies(true);
        setModalVisible(false);
        resetForm();
      };

      if (Platform.OS === 'web') {
        window.alert('Assembleia atualizada com sucesso.');
        await finishUpdate();
        savingRef.current = false;
        setSaving(false);
        return;
      }

      Alert.alert('Sucesso', 'Assembleia atualizada com sucesso.', [
        {
          text: 'OK',
          onPress: finishUpdate,
        },
      ]);
      savingRef.current = false;
      setSaving(false);
    } catch (error) {
      console.error('Erro ao salvar assembleia:', error);
      setFormError(getFriendlyErrorMessage(error, 'Não foi possível salvar a assembleia.'));
      savingRef.current = false;
      setSaving(false);
    }
  };

  const handleDelete = (id: string) => {
    Alert.alert('Remover assembleia', 'Deseja remover esta assembleia?', [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          try {
            await deleteAssembly(id);
            loadAssemblies(true);
          } catch (error) {
            console.error('Erro ao remover assembleia:', error);
            Alert.alert('Erro', getFriendlyErrorMessage(error, 'Não foi possível remover a assembleia.'));
          }
        },
      },
    ]);
  };

  const handleEdit = (item: Assembly) => {
    setEditingId(item.id);
    setForm({
      condominiumId: item.condominiumId,
      title: item.title,
      description: item.description ?? '',
      startDate: formatDateForInput(item.startTime),
      endDate: formatDateForInput(item.endTime),
      status: (item.status === 'en_andamento' ? 'em_andamento' : item.status || 'agendada') as AssemblyStatus,
      topicsText: item.topics?.map(topic => topic.title).join('\n') ?? '',
      minutes: item.minutes ?? '',
    });
    setFormError('');
    setModalVisible(true);
  };

  const renderAssembly = ({ item }: { item: Assembly }) => {
    const normalizedStatus = item.status === 'en_andamento' ? 'em_andamento' : item.status;
    const effectiveStatus =
      ['cancelada', 'encerrada'].includes(normalizedStatus)
        ? normalizedStatus
        : item.voteFinished
          ? 'encerrada'
          : item.voteStarted
            ? 'em_andamento'
            : 'agendada';
    const info = statusInfo(effectiveStatus);
    const topics = item.topics?.map(topic => topic.title).filter(Boolean) ?? [];

    return (
      <View style={styles.card}>
        <View style={styles.cardHeader}>
          <Text style={styles.cardTitle} numberOfLines={2}>{item.title}</Text>
          <View style={[styles.statusBadge, { backgroundColor: info.bg }]}>
            <Text style={[styles.statusText, { color: info.text }]}>{info.label}</Text>
          </View>
        </View>
        {item.description ? <Text style={styles.description}>{item.description}</Text> : null}
        <View style={styles.detailRow}>
          <Ionicons name="business-outline" size={sw(16)} color="#64748b" />
          <Text style={styles.detailText}>{condominiumName(item.condominiumId)}</Text>
        </View>
        <View style={styles.detailRow}>
          <Ionicons name="calendar-outline" size={sw(16)} color="#64748b" />
          <Text style={styles.detailText}>
            {formatDate(item.startTime)} ate {formatDate(item.endTime)}
          </Text>
        </View>
        <View style={styles.detailRow}>
          <Ionicons name="list-outline" size={sw(16)} color="#64748b" />
          <Text style={styles.detailText}>{topics.length || 0} pauta(s)</Text>
        </View>
        {topics.length ? (
          <View style={styles.topicsBox}>
            {topics.slice(0, 3).map((topic, index) => (
              <Text key={`${item.id}-${index}`} style={styles.topicText}>{index + 1}. {topic}</Text>
            ))}
          </View>
        ) : null}
        {canManage ? (
          <View style={styles.actions}>
            <TouchableOpacity onPress={() => handleEdit(item)} style={styles.actionBtn}>
              <Ionicons name="create-outline" size={sw(18)} color="#3b82f6" />
              <Text style={[styles.actionText, { color: '#3b82f6' }]}>Editar</Text>
            </TouchableOpacity>
            <TouchableOpacity onPress={() => handleDelete(item.id)} style={styles.actionBtn}>
              <Ionicons name="trash-outline" size={sw(18)} color="#ef4444" />
              <Text style={[styles.actionText, { color: '#ef4444' }]}>Remover</Text>
            </TouchableOpacity>
          </View>
        ) : null}
      </View>
    );
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.titleHeader}>Assembleias</Text>
        {canManage ? (
          <TouchableOpacity style={styles.addBtn} onPress={() => { resetForm(); setModalVisible(true); }}>
            <Ionicons name="add" size={sw(22)} color="#fff" />
            <Text style={styles.addBtnText}>Nova</Text>
          </TouchableOpacity>
        ) : null}
      </View>

      {canManage ? (
        <View style={styles.filterSection}>
          <Text style={styles.filterLabel}>Condominio</Text>
          <ScrollView horizontal showsHorizontalScrollIndicator={false}>
            <TouchableOpacity
              style={[styles.filterChip, filterCondominiumId === '' && styles.filterChipActive]}
              onPress={() => setFilterCondominiumId('')}
            >
              <Text style={filterCondominiumId === '' ? styles.filterChipActiveText : styles.filterChipText}>Todos</Text>
            </TouchableOpacity>
            {visibleCondominiums.map(condominium => (
              <TouchableOpacity
                key={condominium.id}
                style={[styles.filterChip, filterCondominiumId === condominium.id && styles.filterChipActive]}
                onPress={() => setFilterCondominiumId(condominium.id)}
              >
                <Text style={filterCondominiumId === condominium.id ? styles.filterChipActiveText : styles.filterChipText}>
                  {condominium.name}
                </Text>
              </TouchableOpacity>
            ))}
          </ScrollView>
        </View>
      ) : null}

      <View style={styles.historyFilter}>
        <TouchableOpacity
          style={[styles.filterChip, !showHistory && styles.filterChipActive]}
          onPress={() => setShowHistory(false)}
        >
          <Text style={!showHistory ? styles.filterChipActiveText : styles.filterChipText}>Atuais</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.filterChip, showHistory && styles.filterChipActive]}
          onPress={() => setShowHistory(true)}
        >
          <Text style={showHistory ? styles.filterChipActiveText : styles.filterChipText}>Histórico</Text>
        </TouchableOpacity>
      </View>

      {loading && !refreshing ? (
        <ActivityIndicator size="large" color="#176b87" style={{ marginTop: sw(60) }} />
      ) : listError ? (
        <View style={styles.errorBox}>
          <Ionicons name="alert-circle-outline" size={sw(32)} color="#ef4444" />
          <Text style={styles.errorText}>{listError}</Text>
          <TouchableOpacity style={styles.retryBtn} onPress={() => loadAssemblies()}>
            <Text style={styles.retryText}>Tentar novamente</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={assemblies}
          keyExtractor={item => item.id}
          renderItem={renderAssembly}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={['#176b87']} />}
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Ionicons name="people-outline" size={sw(56)} color="#cbd5e1" />
              <Text style={styles.emptyText}>Nenhuma assembleia encontrada.</Text>
              <Text style={styles.emptySubtext}>
                {canManage ? 'Toque em "Nova" para criar.' : 'Novas assembleias aparecerao aqui.'}
              </Text>
            </View>
          }
          contentContainerStyle={styles.listContent}
        />
      )}

      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>{editingId ? 'Editar Assembleia' : 'Nova Assembleia'}</Text>
            <TouchableOpacity onPress={() => setModalVisible(false)}>
              <Ionicons name="close" size={sw(26)} color="#64748b" />
            </TouchableOpacity>
          </View>

          <ScrollView style={styles.modalBody} keyboardShouldPersistTaps="handled">
            {formError ? (
              <View style={styles.formError}>
                <Ionicons name="alert-circle-outline" size={sw(16)} color="#dc2626" />
                <Text style={styles.formErrorText}>{formError}</Text>
              </View>
            ) : null}

            <Text style={styles.label}>Condominio *</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: sw(12) }}>
              {visibleCondominiums.map(condominium => (
                <TouchableOpacity
                  key={condominium.id}
                  style={[styles.optionButton, form.condominiumId === condominium.id && styles.optionSelected]}
                  onPress={() => setForm({ ...form, condominiumId: condominium.id })}
                >
                  <Text style={[styles.optionText, form.condominiumId === condominium.id && styles.optionSelectedText]}>
                    {condominium.name}
                  </Text>
                </TouchableOpacity>
              ))}
            </ScrollView>

            <Text style={styles.label}>Titulo *</Text>
            <TextInput style={styles.input} value={form.title} onChangeText={title => setForm({ ...form, title })} />

            <Text style={styles.label}>Descricao</Text>
            <TextInput
              style={[styles.input, { minHeight: sw(80), textAlignVertical: 'top' }]}
              multiline
              value={form.description}
              onChangeText={description => setForm({ ...form, description })}
            />

            <Text style={styles.label}>Data inicial da votação *</Text>
            <TextInput style={styles.input} placeholder="DD/MM/AAAA" keyboardType="numeric" maxLength={10} autoCorrect={false} value={form.startDate} onChangeText={startDate => setForm({ ...form, startDate: maskDateBR(startDate) })} />

            <Text style={styles.label}>Data final da votação</Text>
            <TextInput style={styles.input} placeholder="DD/MM/AAAA" keyboardType="numeric" maxLength={10} autoCorrect={false} value={form.endDate} onChangeText={endDate => setForm({ ...form, endDate: maskDateBR(endDate) })} />

            {editingId ? (
              <>
                <Text style={styles.label}>Status</Text>
                <View style={styles.optionsRow}>
                  {STATUS_OPTIONS.map(option => (
                    <TouchableOpacity
                      key={option.value}
                      style={[styles.optionButton, form.status === option.value && styles.optionSelected]}
                      onPress={() => setForm({ ...form, status: option.value })}
                    >
                      <Text style={[styles.optionText, form.status === option.value && styles.optionSelectedText]}>{option.label}</Text>
                    </TouchableOpacity>
                  ))}
                </View>
              </>
            ) : null}

            <Text style={styles.label}>Pautas</Text>
            <TextInput
              style={[styles.input, { minHeight: sw(100), textAlignVertical: 'top' }]}
              multiline
              placeholder={'Uma pauta por linha'}
              value={form.topicsText}
              onChangeText={topicsText => setForm({ ...form, topicsText })}
            />

            <Text style={styles.label}>Ata</Text>
            <TextInput
              style={[styles.input, { minHeight: sw(80), textAlignVertical: 'top' }]}
              multiline
              value={form.minutes}
              onChangeText={minutes => setForm({ ...form, minutes })}
            />

            <TouchableOpacity style={[styles.saveBtn, saving && styles.disabled]} onPress={handleSave} disabled={saving}>
              <Text style={styles.saveBtnText}>{saving ? 'Salvando...' : 'Salvar'}</Text>
            </TouchableOpacity>
          </ScrollView>
        </SafeAreaView>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  header: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingHorizontal: HP, paddingVertical: sw(16),
    backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0',
  },
  titleHeader: { fontSize: sf(20), fontWeight: '700', color: '#1e293b' },
  addBtn: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#176b87', borderRadius: sw(8), paddingHorizontal: sw(14), paddingVertical: sw(8), gap: sw(4) },
  addBtnText: { color: '#fff', fontWeight: '700', fontSize: sf(14) },
  disabled: { opacity: 0.55 },
  filterSection: { backgroundColor: '#fff', padding: sw(16), margin: sw(16), borderRadius: sw(12), shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.05, shadowRadius: 8, elevation: 2 },
  historyFilter: { flexDirection: 'row', paddingHorizontal: sw(16), paddingTop: sw(12) },
  filterLabel: { fontSize: sf(14), fontWeight: '600', color: '#334155', marginBottom: sw(12) },
  filterChip: { paddingHorizontal: sw(14), paddingVertical: sw(8), backgroundColor: '#f1f5f9', borderRadius: sw(20), marginRight: sw(8) },
  filterChipActive: { backgroundColor: '#176b87' },
  filterChipText: { color: '#475569', fontWeight: '500' },
  filterChipActiveText: { color: '#fff', fontWeight: '600' },
  listContent: { padding: sw(16), paddingBottom: sw(80), gap: sw(12) },
  card: { backgroundColor: '#fff', borderRadius: sw(12), padding: sw(16), shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 4, elevation: 2 },
  cardHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: sw(8), gap: sw(8) },
  cardTitle: { fontSize: sf(16), fontWeight: '700', color: '#0f172a', flex: 1 },
  statusBadge: { paddingHorizontal: sw(8), paddingVertical: sw(3), borderRadius: sw(6) },
  statusText: { fontSize: sf(11), fontWeight: '700' },
  description: { fontSize: sf(14), color: '#475569', marginBottom: sw(12) },
  detailRow: { flexDirection: 'row', alignItems: 'center', gap: sw(8), marginBottom: sw(6) },
  detailText: { fontSize: sf(13), color: '#475569', flex: 1 },
  topicsBox: { backgroundColor: '#f8fafc', borderRadius: sw(8), padding: sw(10), marginTop: sw(8) },
  topicText: { fontSize: sf(13), color: '#475569', marginBottom: sw(4) },
  actions: { flexDirection: 'row', justifyContent: 'flex-start', flexWrap: 'wrap', marginTop: sw(12), paddingTop: sw(12), borderTopWidth: 1, borderTopColor: '#e2e8f0', gap: sw(12) },
  actionBtn: { flexDirection: 'row', alignItems: 'center', gap: sw(6), paddingVertical: sw(6), paddingHorizontal: sw(12), borderRadius: sw(20), backgroundColor: '#f1f5f9' },
  actionText: { fontSize: sf(13), fontWeight: '500' },
  errorBox: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: sw(24) },
  errorText: { color: '#64748b', fontSize: sf(14), textAlign: 'center', marginTop: sw(8) },
  retryBtn: { marginTop: sw(16), paddingHorizontal: HP, paddingVertical: sw(10), backgroundColor: '#176b87', borderRadius: sw(8) },
  retryText: { color: '#fff', fontWeight: '600' },
  emptyContainer: { alignItems: 'center', justifyContent: 'center', paddingVertical: sw(64), paddingHorizontal: sw(24) },
  emptyText: { fontSize: sf(16), fontWeight: '600', color: '#64748b', marginTop: sw(16), textAlign: 'center' },
  emptySubtext: { fontSize: sf(14), color: '#94a3b8', marginTop: sw(8), textAlign: 'center' },
  modal: { flex: 1, backgroundColor: '#f8fafc' },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: HP, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0' },
  modalTitle: { fontSize: sf(18), fontWeight: '700', color: '#1e293b' },
  modalBody: { padding: HP },
  formError: { flexDirection: 'row', alignItems: 'center', gap: sw(6), backgroundColor: '#fef2f2', borderRadius: sw(8), padding: sw(12), marginBottom: sw(8) },
  formErrorText: { color: '#dc2626', fontSize: sf(13), flex: 1 },
  label: { fontSize: sf(13), fontWeight: '600', color: '#475569', marginBottom: sw(6), marginTop: sw(12) },
  input: { borderWidth: 1, borderColor: '#e2e8f0', borderRadius: sw(10), padding: sw(12), fontSize: sf(15), backgroundColor: '#fff', marginBottom: sw(4) },
  optionsRow: { flexDirection: 'row', flexWrap: 'wrap', marginBottom: sw(12), gap: sw(8) },
  optionButton: { paddingHorizontal: sw(14), paddingVertical: sw(8), backgroundColor: '#f1f5f9', borderRadius: sw(20), marginRight: sw(8) },
  optionSelected: { backgroundColor: '#176b87' },
  optionText: { color: '#475569', fontWeight: '500' },
  optionSelectedText: { color: '#fff', fontWeight: '600' },
  saveBtn: { backgroundColor: '#176b87', borderRadius: sw(12), padding: sw(16), alignItems: 'center', marginTop: sw(24), marginBottom: sw(40) },
  saveBtnText: { color: '#fff', fontSize: sf(16), fontWeight: '700' },
});
