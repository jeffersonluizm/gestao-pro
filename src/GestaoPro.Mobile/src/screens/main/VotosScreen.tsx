// src/screens/main/VotosScreen.tsx
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  View, FlatList, StyleSheet, TouchableOpacity,
  Modal, Alert, ActivityIndicator, RefreshControl, ScrollView, Platform,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';

import { useAuth } from '../../context/AuthContext';
import {
  Assembly,
  VoteResponse,
  castVote,
  getAssemblies,
  getAssembliesByCondominium,
  getFriendlyErrorMessage,
  getVotesByAssembly,
} from '../../api/client';
import { sw, sf, HP } from '../../utils/responsive';

import { theme } from '../../theme/theme';
import AppText from '../../components/AppText';
import BackButton from '../../components/BackButton';
type VoteValue = string;

function canSeeAllVotes(role?: string) {
  return ['admin', 'sindico'].includes((role ?? '').toLowerCase());
}

function normalizeStatus(status?: string) {
  return (status ?? '').toLowerCase().replace('en_', 'em_');
}

function formatDate(value?: string) {
  if (!value) return '';
  const date = new Date(value);
  return Number.isFinite(date.getTime())
    ? date.toLocaleDateString('pt-BR')
    : value;
}

function voteIcon(vote: string) {
  const normalized = vote.toLowerCase();
  if (normalized === 'sim') return <Ionicons name="checkmark-circle" size={sw(22)} color={theme.color.success} />;
  if (normalized === 'não' || normalized === 'nao') return <Ionicons name="close-circle" size={sw(22)} color={theme.color.danger} />;
  return <Ionicons name="lock-closed-outline" size={sw(22)} color={theme.color.inkMuted} />;
}

function getTopicVoteOptions(assembly?: Assembly, topicIndex = 0) {
  const topic = assembly?.topics?.[topicIndex];
  const options = topic?.voteOptions?.map(option => option.trim()).filter(Boolean) ?? [];
  return options.length ? options : ['sim', 'não'];
}

function getVoteAvailability(assembly?: Assembly) {
  if (!assembly) return { canVote: false, message: 'Selecione uma assembleia para votar.' };

  const now = Date.now();
  const start = new Date(assembly.startTime).getTime();
  const end = new Date(assembly.endTime).getTime();

  if (Number.isFinite(start) && now < start) {
    return { canVote: false, message: 'A votação ainda não foi iniciada.' };
  }
  if (Number.isFinite(end) && now > end) {
    return { canVote: false, message: 'A votação já foi encerrada.' };
  }
  if (['encerrada', 'cancelada'].includes(normalizeStatus(assembly.status))) {
    return { canVote: false, message: 'A votação desta assembleia não está ativa no momento.' };
  }
  return { canVote: true, message: '' };
}

function canAssemblyReceiveVote(assembly?: Assembly) {
  return getVoteAvailability(assembly).canVote;
}

export default function VotosScreen() {
  const { session } = useAuth();
  const canSeeAll = canSeeAllVotes(session?.role);

  const [assemblies, setAssemblies] = useState<Assembly[]>([]);
  const [votes, setVotes] = useState<VoteResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [listError, setListError] = useState('');
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [selectedAssemblyId, setSelectedAssemblyId] = useState('');
  const [form, setForm] = useState<{ assemblyId: string; topicIndex: number; vote: VoteValue }>({
    assemblyId: '',
    topicIndex: 0,
    vote: 'sim',
  });

  const selectedAssembly = useMemo(
    () => assemblies.find(assembly => assembly.id === selectedAssemblyId),
    [assemblies, selectedAssemblyId],
  );

  const formAssembly = useMemo(
    () => assemblies.find(assembly => assembly.id === form.assemblyId),
    [assemblies, form.assemblyId],
  );

  const formTopic = formAssembly?.topics?.[form.topicIndex];
  const formVoteOptions = useMemo(
    () => getTopicVoteOptions(formAssembly, form.topicIndex),
    [formAssembly, form.topicIndex],
  );
  const individualVotes = useMemo(() => votes.filter(vote => !vote.summaryOnly), [votes]);

  const loadVotes = useCallback(async (silent = false, preferredAssemblyId = selectedAssemblyId) => {
    if (!session?.condominiumId) return;
    if (!silent) setLoading(true);
    setListError('');

    try {
      const assembliesData = canSeeAll
        ? await getAssemblies()
        : await getAssembliesByCondominium(session.condominiumId);

      const sortedAssemblies = assembliesData.sort((a, b) => new Date(b.startTime).getTime() - new Date(a.startTime).getTime());
      setAssemblies(sortedAssemblies);

      const nextAssemblyId = preferredAssemblyId || sortedAssemblies[0]?.id || '';
      if (!nextAssemblyId) {
        setSelectedAssemblyId('');
        setVotes([]);
        return;
      }

      if (nextAssemblyId !== selectedAssemblyId) {
        setSelectedAssemblyId(nextAssemblyId);
      }

      const data = await getVotesByAssembly(nextAssemblyId);
      setVotes(data.sort((a, b) => new Date(b.createdAt ?? '').getTime() - new Date(a.createdAt ?? '').getTime()));
    } catch (error) {
      console.error('Erro ao carregar votos:', error);
      setListError(getFriendlyErrorMessage(error, 'Não foi possível carregar os votos.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [canSeeAll, selectedAssemblyId, session?.condominiumId]);

  useFocusEffect(useCallback(() => { loadVotes(); }, [loadVotes]));
  useEffect(() => { loadVotes(); }, [loadVotes]);

  const onRefresh = () => {
    setRefreshing(true);
    loadVotes(true);
  };

  const openVoteModal = () => {
    const assemblyId = selectedAssemblyId || assemblies[0]?.id || '';
    const assembly = assemblies.find(item => item.id === assemblyId);
    const vote = getTopicVoteOptions(assembly, 0)[0] ?? 'sim';
    setForm({ assemblyId, topicIndex: 0, vote });
    setFormError('');
    setModalVisible(true);
  };

  const handleFormAssemblyChange = (assemblyId: string) => {
    const assembly = assemblies.find(item => item.id === assemblyId);
    const vote = getTopicVoteOptions(assembly, 0)[0] ?? 'sim';
    setForm({ assemblyId, topicIndex: 0, vote });
  };

  const handleTopicChange = (topicIndex: number) => {
    const vote = getTopicVoteOptions(formAssembly, topicIndex)[0] ?? 'sim';
    setForm({ ...form, topicIndex, vote });
  };

  const handleSave = async () => {
    if (saving) return;
    setFormError('');
    if (!form.assemblyId) {
      setFormError('Selecione uma assembleia para votar.');
      return;
    }
    if (!session?.userId) {
      setFormError('Sessao invalida. Entre novamente.');
      return;
    }

    const targetAssembly = assemblies.find(assembly => assembly.id === form.assemblyId);
    const availability = getVoteAvailability(targetAssembly);
    if (!availability.canVote) {
      setFormError(availability.message);
      return;
    }

    // Verifica se já votou na pauta selecionada nos dados carregados
    const alreadyVoted = votes.some(v =>
      v.assemblyId === form.assemblyId &&
      v.topicIndex === form.topicIndex &&
      v.userId === session.userId &&
      v.voto !== 'Privado'
    );
    if (alreadyVoted) {
      setFormError('Você já registrou um voto nesta pauta.');
      return;
    }

    setSaving(true);
    try {
      await castVote({
        assemblyId: form.assemblyId,
        topicIndex: form.topicIndex,
        vote: form.vote,
      });

      const finishVote = async () => {
        await loadVotes(true, form.assemblyId);
        setModalVisible(false);
      };

      if (Platform.OS === 'web') {
        window.alert('Voto registrado.');
        await finishVote();
        return;
      }

      Alert.alert('Sucesso', 'Voto registrado.', [
        {
          text: 'OK',
          onPress: finishVote,
        },
      ]);
    } catch (error) {
      console.error('Erro ao registrar voto:', error);
      const msg = getFriendlyErrorMessage(error, 'Não foi possível registrar o voto.');
      // Traduz mensagens do backend para português amigável
      if (msg.includes('já votou') || msg.includes('409')) {
        setFormError('Você já registrou um voto nesta pauta.');
      } else if (msg.includes('não foi iniciada')) {
        setFormError('A votação ainda não foi iniciada.');
      } else if (msg.includes('já foi encerrada')) {
        setFormError('A votação já foi encerrada.');
      } else if (msg.includes('não está ativa')) {
        setFormError('Esta assembleia não está em período de votação.');
      } else if (msg.includes('403')) {
        setFormError('Você delegou seu voto e não pode votar diretamente.');
      } else {
        setFormError(msg);
      }
    } finally {
      setSaving(false);
    }
  };

  const handleSelectAssembly = (assemblyId: string) => {
    setSelectedAssemblyId(assemblyId);
    loadVotes(true, assemblyId);
  };

  const renderVote = ({ item }: { item: VoteResponse }) => (
    <View style={styles.card}>
      <View style={styles.cardHeader}>
        <AppText style={styles.cardTitle} numberOfLines={2}>{item.assemblyTitle || selectedAssembly?.title || 'Assembleia'}</AppText>
        {voteIcon(item.voto)}
      </View>
      {item.topicDescription ? (
        <View style={styles.detailRow}>
          <Ionicons name="document-text-outline" size={sw(16)} color={theme.color.inkMuted} />
          <AppText style={styles.detailText}>{item.topicDescription}</AppText>
        </View>
      ) : null}
      {canSeeAll ? (
        <View style={styles.detailRow}>
          <Ionicons name="person-outline" size={sw(16)} color={theme.color.inkMuted} />
          <AppText style={styles.detailText}>Usuario: {item.userId}</AppText>
        </View>
      ) : null}
      <View style={styles.detailRow}>
        <Ionicons name="checkbox-outline" size={sw(16)} color={theme.color.inkMuted} />
        <AppText style={styles.detailText}>Voto: {item.voto}</AppText>
      </View>
      {item.createdAt ? (
        <View style={styles.detailRow}>
          <Ionicons name="time-outline" size={sw(16)} color={theme.color.inkMuted} />
          <AppText style={styles.detailText}>{new Date(item.createdAt).toLocaleDateString('pt-BR')}</AppText>
        </View>
      ) : null}
    </View>
  );

  const renderAssemblySummary = () => {
    if (!selectedAssembly) return null;
    const voteOpen = canAssemblyReceiveVote(selectedAssembly);
    const statusLabel = selectedAssembly.voteStatus || (voteOpen ? 'Em andamento' : selectedAssembly.status);

    return (
      <View style={styles.summary}>
        <View style={styles.summaryHeader}>
          <AppText style={styles.summaryTitle}>{selectedAssembly.title}</AppText>
          <View style={[styles.statusBadge, voteOpen ? styles.statusOpen : styles.statusClosed]}>
            <AppText style={[styles.statusText, voteOpen ? styles.statusOpenText : styles.statusClosedText]}>
              {statusLabel}
            </AppText>
          </View>
        </View>
        {selectedAssembly.description ? <AppText style={styles.summaryText}>{selectedAssembly.description}</AppText> : null}
        <AppText style={styles.summaryMeta}>
          {formatDate(selectedAssembly.startTime)} ate {formatDate(selectedAssembly.endTime)}
        </AppText>
        {selectedAssembly.topics.map((topic, index) => {
          const result = votes.find(vote => vote.topicIndex === index);
          const counts = result?.optionCounts ?? {};
          return (
            <View style={styles.topicResult} key={`${selectedAssembly.id}-${index}`}>
              <AppText style={styles.topicResultTitle}>{topic.title || `Pauta ${index + 1}`}</AppText>
              {Object.keys(counts).length
                ? Object.entries(counts).map(([option, count]) => (
                    <AppText style={styles.resultText} key={option}>{option}: {count}</AppText>
                  ))
                : <AppText style={styles.resultText}>Nenhum voto registrado.</AppText>}
              <AppText style={styles.resultText}>Total da pauta: {result?.totalVotes ?? 0}</AppText>
            </View>
          );
        })}
      </View>
    );
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <BackButton />
        <AppText style={styles.titleHeader}>Votos</AppText>
        <TouchableOpacity accessibilityRole="button" style={styles.addBtn} onPress={openVoteModal}>
          <Ionicons name="checkbox-outline" size={sw(20)} color="#fff" />
          <AppText style={styles.addBtnText}>Votar</AppText>
        </TouchableOpacity>
      </View>

      <View style={styles.filterSection}>
        <AppText style={styles.filterLabel}>Assembleia</AppText>
        <ScrollView horizontal showsHorizontalScrollIndicator={false}>
          {assemblies.map(assembly => (
            <TouchableOpacity accessibilityRole="button"
              key={assembly.id}
              style={[styles.filterChip, selectedAssemblyId === assembly.id && styles.filterChipActive]}
              onPress={() => handleSelectAssembly(assembly.id)}
            >
              <AppText style={selectedAssemblyId === assembly.id ? styles.filterChipActiveText : styles.filterChipText}>
                {assembly.title}
              </AppText>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>

      {loading && !refreshing ? (
        <ActivityIndicator size="large" color={theme.color.action} style={{ marginTop: sw(60) }} />
      ) : listError ? (
        <View style={styles.errorBox}>
          <Ionicons name="alert-circle-outline" size={sw(32)} color={theme.color.danger} />
          <AppText style={styles.errorText}>{listError}</AppText>
          <TouchableOpacity accessibilityRole="button" style={styles.retryBtn} onPress={() => loadVotes()}>
            <AppText style={styles.retryText}>Tentar novamente</AppText>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={individualVotes}
          keyExtractor={item => item.id}
          renderItem={renderVote}
          ListHeaderComponent={renderAssemblySummary}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={[theme.color.action]} />}
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Ionicons name="checkbox-outline" size={sw(56)} color={theme.color.borderStrong} />
              <AppText style={styles.emptyText}>Nenhum voto encontrado.</AppText>
              <AppText style={styles.emptySubtext}>Use o botao "Votar" para registrar seu voto.</AppText>
            </View>
          }
          contentContainerStyle={styles.listContent}
        />
      )}

      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <AppText style={styles.modalTitle}>Novo Voto</AppText>
            <TouchableOpacity accessibilityRole="button" onPress={() => setModalVisible(false)}>
              <Ionicons name="close" size={sw(26)} color={theme.color.inkMuted} />
            </TouchableOpacity>
          </View>
          <ScrollView style={styles.modalBody} keyboardShouldPersistTaps="handled">
            {formError ? (
              <View style={styles.formError}>
                <Ionicons name="alert-circle-outline" size={sw(16)} color={theme.color.danger} />
                <AppText style={styles.formErrorText}>{formError}</AppText>
              </View>
            ) : null}

            <AppText style={styles.label}>Assembleia *</AppText>
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: sw(12) }}>
              {assemblies.map(assembly => (
                <TouchableOpacity accessibilityRole="button"
                  key={assembly.id}
                  style={[styles.optionButton, form.assemblyId === assembly.id && styles.optionSelected]}
                  onPress={() => handleFormAssemblyChange(assembly.id)}
                >
                  <AppText style={[styles.optionText, form.assemblyId === assembly.id && styles.optionSelectedText]}>
                    {assembly.title}
                  </AppText>
                </TouchableOpacity>
              ))}
            </ScrollView>

            {formAssembly?.topics?.length ? (
              <>
                <AppText style={styles.label}>Pauta *</AppText>
                <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: sw(12) }}>
                  {formAssembly.topics.map((topic, index) => (
                    <TouchableOpacity accessibilityRole="button"
                      key={`${formAssembly.id}-${index}`}
                      style={[styles.optionButton, form.topicIndex === index && styles.optionSelected]}
                      onPress={() => handleTopicChange(index)}
                    >
                      <AppText style={[styles.optionText, form.topicIndex === index && styles.optionSelectedText]}>
                        {topic.title || `Pauta ${index + 1}`}
                      </AppText>
                    </TouchableOpacity>
                  ))}
                </ScrollView>
                {formTopic?.desc ? <AppText style={styles.topicDescription}>{formTopic.desc}</AppText> : null}
              </>
            ) : null}

            <AppText style={styles.label}>Voto *</AppText>
            <View style={styles.optionsRow}>
              {formVoteOptions.map(option => {
                const selected = form.vote === option;
                return (
                  <TouchableOpacity accessibilityRole="button"
                    key={option}
                    style={[styles.voteOption, selected && styles.voteSelected]}
                    onPress={() => setForm({ ...form, vote: option })}
                  >
                    <Ionicons name={selected ? 'radio-button-on' : 'radio-button-off'} size={sw(18)} color={selected ? '#fff' : theme.color.action} />
                    <AppText style={[styles.voteOptionText, selected && { color: '#fff' }]}>{option}</AppText>
                  </TouchableOpacity>
                );
              })}
            </View>

            <TouchableOpacity accessibilityRole="button" style={styles.saveBtn} onPress={handleSave} disabled={saving}>
              {saving ? <ActivityIndicator color="#fff" /> : <AppText style={styles.saveBtnText}>Salvar voto</AppText>}
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
    paddingHorizontal: HP, paddingVertical: sw(16),
    backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: theme.color.border,
  },
  titleHeader: { fontSize: sf(20), fontWeight: '700', color: theme.color.ink },
  addBtn: { flexDirection: 'row', alignItems: 'center', backgroundColor: theme.color.action, borderRadius: sw(8), paddingHorizontal: sw(14), paddingVertical: sw(8), gap: sw(6) },
  addBtnText: { color: '#fff', fontWeight: '700', fontSize: sf(14) },
  filterSection: { backgroundColor: '#fff', padding: sw(16), margin: sw(16), borderRadius: sw(12), shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.05, shadowRadius: 8, elevation: 2 },
  filterLabel: { fontSize: sf(14), fontWeight: '600', color: theme.color.ink, marginBottom: sw(12) },
  filterChip: { paddingHorizontal: sw(14), paddingVertical: sw(8), backgroundColor: theme.color.surfaceSoft, borderRadius: sw(20), marginRight: sw(8) },
  filterChipActive: { backgroundColor: theme.color.action },
  filterChipText: { color: theme.color.inkMuted, fontWeight: '500' },
  filterChipActiveText: { color: '#fff', fontWeight: '600' },
  listContent: { padding: sw(16), paddingBottom: sw(80), gap: sw(12) },
  summary: { backgroundColor: '#fff', borderRadius: sw(12), padding: sw(16), marginBottom: sw(12), borderWidth: 1, borderColor: theme.color.border },
  summaryHeader: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: sw(8) },
  summaryTitle: { fontSize: sf(16), fontWeight: '700', color: theme.color.ink, flex: 1 },
  summaryText: { fontSize: sf(14), color: theme.color.inkMuted, marginTop: sw(8) },
  summaryMeta: { fontSize: sf(13), color: theme.color.inkMuted, marginTop: sw(8) },
  statusBadge: { borderRadius: sw(6), paddingHorizontal: sw(8), paddingVertical: sw(3) },
  statusOpen: { backgroundColor: theme.color.successSoft },
  statusClosed: { backgroundColor: theme.color.warningSoft },
  statusText: { fontSize: sf(11), fontWeight: '700' },
  statusOpenText: { color: theme.color.success },
  statusClosedText: { color: theme.color.warning },
  resultRow: { flexDirection: 'row', flexWrap: 'wrap', gap: sw(8), marginTop: sw(12) },
  topicResult: { marginTop: sw(12), gap: sw(6), borderTopWidth: 1, borderTopColor: theme.color.border, paddingTop: sw(10) },
  topicResultTitle: { fontSize: sf(14), fontWeight: '700', color: theme.color.ink },
  resultText: { backgroundColor: theme.color.surfaceSoft, borderRadius: sw(8), paddingHorizontal: sw(10), paddingVertical: sw(6), color: theme.color.ink, fontSize: sf(13), fontWeight: '600' },
  card: { backgroundColor: '#fff', borderRadius: sw(12), padding: sw(16), shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 4, elevation: 2 },
  cardHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: sw(8), gap: sw(8) },
  cardTitle: { fontSize: sf(16), fontWeight: '700', color: theme.color.ink, flex: 1 },
  detailRow: { flexDirection: 'row', alignItems: 'center', gap: sw(8), marginBottom: sw(6) },
  detailText: { fontSize: sf(13), color: theme.color.inkMuted, flex: 1 },
  errorBox: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: sw(24) },
  errorText: { color: theme.color.inkMuted, fontSize: sf(14), textAlign: 'center', marginTop: sw(8) },
  retryBtn: { marginTop: sw(16), paddingHorizontal: HP, paddingVertical: sw(10), backgroundColor: theme.color.action, borderRadius: sw(8) },
  retryText: { color: '#fff', fontWeight: '600' },
  emptyContainer: { alignItems: 'center', justifyContent: 'center', paddingVertical: sw(64), paddingHorizontal: sw(24) },
  emptyText: { fontSize: sf(16), fontWeight: '600', color: theme.color.inkMuted, marginTop: sw(16), textAlign: 'center' },
  emptySubtext: { fontSize: sf(14), color: theme.color.inkMuted, marginTop: sw(8), textAlign: 'center' },
  modal: { flex: 1, backgroundColor: theme.color.background },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: HP, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: theme.color.border },
  modalTitle: { fontSize: sf(18), fontWeight: '700', color: theme.color.ink },
  modalBody: { padding: HP },
  formError: { flexDirection: 'row', alignItems: 'center', gap: sw(6), backgroundColor: theme.color.dangerSoft, borderRadius: sw(8), padding: sw(12), marginBottom: sw(8) },
  formErrorText: { color: theme.color.danger, fontSize: sf(13), flex: 1 },
  label: { fontSize: sf(13), fontWeight: '600', color: theme.color.inkMuted, marginBottom: sw(6), marginTop: sw(12) },
  optionsRow: { flexDirection: 'row', flexWrap: 'wrap', marginBottom: sw(12), gap: sw(8) },
  optionButton: { paddingHorizontal: sw(14), paddingVertical: sw(8), backgroundColor: theme.color.surfaceSoft, borderRadius: sw(20), marginRight: sw(8) },
  optionSelected: { backgroundColor: theme.color.action },
  optionText: { color: theme.color.inkMuted, fontWeight: '500' },
  optionSelectedText: { color: '#fff', fontWeight: '600' },
  voteOption: { flexDirection: 'row', alignItems: 'center', gap: sw(6), paddingHorizontal: sw(16), paddingVertical: sw(10), backgroundColor: '#fff', borderRadius: sw(10), borderWidth: 1, borderColor: theme.color.border },
  voteSelected: { backgroundColor: theme.color.action, borderColor: theme.color.action },
  topicDescription: { fontSize: sf(13), color: theme.color.inkMuted, marginBottom: sw(8) },
  voteOptionText: { fontSize: sf(14), color: theme.color.inkMuted, fontWeight: '700' },
  saveBtn: { backgroundColor: theme.color.action, borderRadius: sw(12), padding: sw(16), alignItems: 'center', marginTop: sw(24), marginBottom: sw(40) },
  saveBtnText: { color: '#fff', fontSize: sf(16), fontWeight: '700' },
});
