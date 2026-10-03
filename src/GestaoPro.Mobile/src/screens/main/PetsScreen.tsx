// src/screens/main/PetsScreen.tsx
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  Modal, TextInput, ScrollView, ActivityIndicator, RefreshControl,
  Switch, Alert, Platform,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';

import { useAuth } from '../../context/AuthContext';
import {
  createPet,
  deletePet,
  getPetsByCondominium,
  getPetsByOwner,
  getPetsByUnit,
  getUnitsByCondominium,
  getUserById,
  getUsersByCondominium,
  getFriendlyErrorMessage,
  Pet,
  PetSpecies,
  Unit,
  updatePet,
  User,
} from '../../api/client';

const SPECIES_OPTIONS: { label: string; value: PetSpecies }[] = [
  { label: 'Cao', value: 'Dog' },
  { label: 'Gato', value: 'Cat' },
  { label: 'Passaro', value: 'Bird' },
  { label: 'Coelho', value: 'Rabbit' },
  { label: 'Outro', value: 'Other' },
];

const SPECIES_LABEL: Record<PetSpecies, string> = {
  Dog: 'Cao',
  Cat: 'Gato',
  Bird: 'Passaro',
  Rabbit: 'Coelho',
  Other: 'Outro',
};

function canManagePets(role?: string) {
  const normalizedRole = (role ?? '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
  return ['admin', 'administrador', 'sindico'].includes(normalizedRole);
}

function formatUnit(unit?: Unit) {
  if (!unit) return '';
  return [unit.block, unit.number].filter(Boolean).join(' ') || unit.id;
}

function emptyForm(condominiumId = '') {
  return {
    ownerId: '',
    unitId: '',
    condominiumId,
    name: '',
    species: 'Dog' as PetSpecies,
    breed: '',
    color: '',
    vaccination: false,
    observations: '',
  };
}

function petId(pet: Pet) {
  const mongoId = typeof pet._id === 'object' && pet._id !== null
    ? pet._id.$oid
    : pet._id;
  return [pet.id, mongoId]
    .map(value => String(value ?? '').trim())
    .find(Boolean) ?? '';
}

export default function PetsScreen() {
  const { session } = useAuth();
  const canManage = canManagePets(session?.role);

  const [pets, setPets] = useState<Pet[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [units, setUnits] = useState<Unit[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [listError, setListError] = useState('');
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState(() => emptyForm(session?.condominiumId));

  const residents = useMemo(
    () => users.filter(user => ['morador', 'sindico'].includes((user.role ?? '').toLowerCase())),
    [users],
  );
  const selectedTutor = useMemo(
    () => residents.find(user => user.id === form.ownerId),
    [form.ownerId, residents],
  );
  const selectedUnit = useMemo(
    () => units.find(unit => unit.id === form.unitId),
    [form.unitId, units],
  );
  const tutorUnits = useMemo(
    () => form.ownerId ? units.filter(unit => unit.residentIds?.includes(form.ownerId)) : [],
    [form.ownerId, units],
  );

  const loadPets = useCallback(async (silent = false) => {
    if (!session?.condominiumId || !session?.userId) return;
    if (!silent) setLoading(true);
    setListError('');

    try {
      const [usersData, unitsData] = await Promise.all([
        getUsersByCondominium(session.condominiumId),
        getUnitsByCondominium(session.condominiumId),
      ]);
      setUsers(usersData);
      setUnits(unitsData);

      let data: Pet[];
      if (canManage) {
        data = await getPetsByCondominium(session.condominiumId);
      } else {
        const currentUser = usersData.find(user => user.id === session.userId)
          ?? await getUserById(session.userId).catch(() => null);
        data = currentUser?.unitId
          ? await getPetsByUnit(currentUser.unitId)
          : await getPetsByOwner(session.userId);
      }

      setPets(data.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()));
    } catch (error: any) {
      console.error('Erro ao carregar pets:', error);
      setListError(getFriendlyErrorMessage(error, 'Não foi possível carregar os pets.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [canManage, session?.condominiumId, session?.userId]);

  useFocusEffect(useCallback(() => { loadPets(); }, [loadPets]));
  useEffect(() => { loadPets(); }, [loadPets]);

  const onRefresh = () => {
    setRefreshing(true);
    loadPets(true);
  };

  const resetForm = () => {
    setEditingId(null);
    setForm(emptyForm(session?.condominiumId));
    setFormError('');
  };

  const ownerName = (id: string) =>
    users.find(user => user.id === id)?.name ?? id;

  const unitName = (id: string) =>
    formatUnit(units.find(unit => unit.id === id)) || id;

  const unitForResident = (residentId: string) =>
    units.find(unit => unit.residentIds?.includes(residentId));

  const handleSave = async () => {
    if (saving) return;
    if (!canManage) return;
    setFormError('');

    if (!form.ownerId) { setFormError('Selecione o tutor do pet.'); return; }
    if (!form.unitId) { setFormError('Selecione a unidade do pet.'); return; }
    if (!form.condominiumId) { setFormError('Selecione o condomínio do pet.'); return; }
    if (!form.name.trim()) { setFormError('Informe o nome do pet.'); return; }
    if (!form.species) { setFormError('Selecione a espécie.'); return; }
    if (!selectedTutor || selectedTutor.condominiumId !== form.condominiumId) {
      setFormError('O tutor selecionado não pertence ao condomínio escolhido.');
      return;
    }
    if (!selectedUnit || selectedUnit.condominiumId !== form.condominiumId) {
      setFormError('A unidade selecionada não pertence ao condomínio escolhido.');
      return;
    }
    if (!selectedUnit.residentIds?.includes(form.ownerId)) {
      setFormError('O morador selecionado não pertence à unidade escolhida.');
      return;
    }

    const isEditing = Boolean(editingId);
    setSaving(true);
    try {
      if (editingId) {
        await updatePet(editingId, {
          name: form.name.trim(),
          species: form.species,
          breed: form.breed.trim(),
          color: form.color.trim(),
          vaccination: form.vaccination,
          observations: form.observations.trim(),
        });
      } else {
        const payload = {
          ownerId: form.ownerId,
          unitId: form.unitId,
          condominiumId: form.condominiumId,
          name: form.name.trim(),
          species: form.species,
          breed: form.breed.trim(),
          color: form.color.trim(),
          vaccination: form.vaccination,
          observations: form.observations.trim(),
        };
        await createPet(payload);
      }

      const successMessage = isEditing ? 'Pet atualizado com sucesso.' : 'Pet criado com sucesso.';
      const finishSave = async () => {
        await loadPets(true);
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
      console.error('Erro ao salvar pet:', error);
      setFormError(getFriendlyErrorMessage(error, 'Não foi possível salvar o pet.'));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = (pet: Pet) => {
    const id = petId(pet);
    if (!id) {
      Alert.alert('Erro', 'Não foi possível identificar o pet para remoção.');
      return;
    }

    Alert.alert('Confirmar remoção', 'Deseja realmente remover este pet?', [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          try {
            await deletePet(id);
            Alert.alert('Sucesso', 'Pet removido com sucesso.', [
              {
                text: 'OK',
                onPress: () => loadPets(true),
              },
            ]);
          } catch (error) {
            console.error('Erro ao remover pet:', error);
            Alert.alert('Erro', getFriendlyErrorMessage(error, 'Não foi possível remover o pet.'));
          }
        },
      },
    ]);
  };

  const handleEdit = (pet: Pet) => {
    setEditingId(pet.id);
    setForm({
      ownerId: pet.ownerId,
      unitId: pet.unitId,
      condominiumId: pet.condominiumId,
      name: pet.name,
      species: pet.species ?? 'Other',
      breed: pet.breed ?? '',
      color: pet.color ?? '',
      vaccination: pet.vaccination,
      observations: pet.observations ?? '',
    });
    setFormError('');
    setModalVisible(true);
  };

  const renderPet = ({ item }: { item: Pet }) => {
    const speciesLabel = SPECIES_LABEL[item.species] ?? item.species;

    return (
      <View style={styles.card}>
        <View style={styles.cardRow}>
          <Text style={styles.cardTitle}>{item.name}</Text>
          <View style={[styles.badge, item.vaccination ? styles.badgeSuccess : styles.badgeWarning]}>
            <Text style={[styles.badgeText, item.vaccination ? styles.badgeSuccessText : styles.badgeWarningText]}>
              {item.vaccination ? 'Vacinado' : 'Nao vacinado'}
            </Text>
          </View>
        </View>
        <Text style={styles.cardMeta}>Tutor: {ownerName(item.ownerId)} - Unidade {unitName(item.unitId)}</Text>
        <Text style={styles.cardMeta}>Especie: {speciesLabel}</Text>
        <Text style={styles.cardMeta}>Raca: {item.breed || '-'} | Cor: {item.color || '-'}</Text>
        {item.observations ? <Text style={styles.cardObs}>Obs: {item.observations}</Text> : null}
        {canManage ? (
          <View style={styles.actionRow}>
            <TouchableOpacity onPress={() => handleEdit(item)} style={styles.actionBtn}>
              <Ionicons name="create-outline" size={18} color="#3b82f6" />
              <Text style={{ color: '#3b82f6' }}>Editar</Text>
            </TouchableOpacity>
            <TouchableOpacity onPress={() => handleDelete(item)} style={styles.actionBtn}>
              <Ionicons name="trash-outline" size={18} color="#ef4444" />
              <Text style={{ color: '#ef4444' }}>Remover</Text>
            </TouchableOpacity>
          </View>
        ) : null}
      </View>
    );
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Pets</Text>
        {canManage ? (
          <TouchableOpacity style={styles.addBtn} onPress={() => { resetForm(); setModalVisible(true); }}>
            <Ionicons name="add" size={22} color="#fff" />
            <Text style={styles.addBtnText}>Novo</Text>
          </TouchableOpacity>
        ) : null}
      </View>

      {loading && !refreshing ? (
        <ActivityIndicator size="large" color="#176b87" style={{ marginTop: 60 }} />
      ) : listError ? (
        <View style={styles.errorBox}>
          <Ionicons name="alert-circle-outline" size={32} color="#ef4444" />
          <Text style={styles.errorText}>{listError}</Text>
          <TouchableOpacity style={styles.retryBtn} onPress={() => loadPets()}>
            <Text style={styles.retryText}>Tentar novamente</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={pets}
          keyExtractor={(item, index) => petId(item) || String(index)}
          renderItem={renderPet}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={['#176b87']} />}
          contentContainerStyle={styles.list}
          ListEmptyComponent={
            <View style={styles.empty}>
              <Ionicons name="paw-outline" size={48} color="#cbd5e1" />
              <Text style={styles.emptyText}>Nenhum pet cadastrado.</Text>
              <Text style={styles.emptySubtext}>
                {canManage ? 'Toque em "Novo" para adicionar.' : 'Pets da sua unidade aparecerao aqui.'}
              </Text>
            </View>
          }
        />
      )}

      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>{editingId ? 'Editar Pet' : 'Novo Pet'}</Text>
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

            <Text style={styles.label}>Tutor *</Text>
            {residents.length ? (
              <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 12 }}>
                {residents.map(user => (
                  <TouchableOpacity
                    key={user.id}
                    style={[styles.areaChip, form.ownerId === user.id && styles.areaChipActive]}
                    onPress={() => {
                      const linkedUnit = unitForResident(user.id);
                      setForm(prev => ({ ...prev, ownerId: user.id, unitId: linkedUnit?.id ?? '' }));
                    }}
                  >
                    <Text style={[styles.areaChipText, form.ownerId === user.id && { color: '#fff' }]}>
                      {user.name}{unitForResident(user.id)?.id
                        ? ` - ${unitName(unitForResident(user.id)?.id ?? '')}`
                        : ''}
                    </Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            ) : (
              <Text style={styles.helpText}>Nenhum morador encontrado neste condominio.</Text>
            )}

            <Text style={styles.label}>Unidade *</Text>
            {tutorUnits.length ? (
              <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 12 }}>
                {tutorUnits.map(unit => (
                  <TouchableOpacity
                    key={unit.id}
                    style={[styles.areaChip, form.unitId === unit.id && styles.areaChipActive]}
                    onPress={() => setForm(prev => ({ ...prev, unitId: unit.id }))}
                  >
                    <Text style={[styles.areaChipText, form.unitId === unit.id && { color: '#fff' }]}>
                      {formatUnit(unit)}
                    </Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            ) : (
              <Text style={styles.helpText}>
                {form.ownerId
                  ? 'O tutor selecionado não está vinculado a nenhuma unidade.'
                  : 'Selecione um tutor para visualizar as unidades disponíveis.'}
              </Text>
            )}

            <Text style={styles.label}>Nome do pet *</Text>
            <TextInput style={styles.input} value={form.name} onChangeText={name => setForm({ ...form, name })} />

            <Text style={styles.label}>Especie *</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 12 }}>
              {SPECIES_OPTIONS.map(option => (
                <TouchableOpacity
                  key={option.value}
                  style={[styles.areaChip, form.species === option.value && styles.areaChipActive]}
                  onPress={() => setForm({ ...form, species: option.value })}
                >
                  <Text style={[styles.areaChipText, form.species === option.value && { color: '#fff' }]}>{option.label}</Text>
                </TouchableOpacity>
              ))}
            </ScrollView>

            <Text style={styles.label}>Raca</Text>
            <TextInput style={styles.input} value={form.breed} onChangeText={breed => setForm({ ...form, breed })} />

            <Text style={styles.label}>Cor</Text>
            <TextInput style={styles.input} value={form.color} onChangeText={color => setForm({ ...form, color })} />

            <View style={styles.switchRow}>
              <Text style={styles.label}>Vacinado</Text>
              <Switch
                value={form.vaccination}
                onValueChange={vaccination => setForm({ ...form, vaccination })}
                trackColor={{ false: '#e2e8f0', true: '#176b87' }}
              />
            </View>

            <Text style={styles.label}>Observacoes</Text>
            <TextInput
              style={[styles.input, { height: 80, textAlignVertical: 'top' }]}
              multiline
              value={form.observations}
              onChangeText={observations => setForm({ ...form, observations })}
            />

            <TouchableOpacity style={styles.saveBtn} onPress={handleSave} disabled={saving}>
              {saving ? <ActivityIndicator color="#fff" /> : <Text style={styles.saveBtnText}>Salvar</Text>}
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
    paddingHorizontal: 20, paddingVertical: 16,
    backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0',
  },
  title: { fontSize: 20, fontWeight: '700', color: '#1e293b' },
  addBtn: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#176b87', borderRadius: 8, paddingHorizontal: 14, paddingVertical: 8, gap: 4 },
  addBtnText: { color: '#fff', fontWeight: '700', fontSize: 14 },
  list: { padding: 16, paddingBottom: 80, gap: 12 },
  card: { backgroundColor: '#fff', borderRadius: 14, padding: 16, shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 4, elevation: 2 },
  cardRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8, gap: 8 },
  cardTitle: { fontSize: 16, fontWeight: '700', color: '#1e293b', flex: 1 },
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 2 },
  badgeSuccess: { backgroundColor: '#dcfce7' },
  badgeWarning: { backgroundColor: '#fef3c7' },
  badgeText: { fontSize: 11, fontWeight: '700' },
  badgeSuccessText: { color: '#15803d' },
  badgeWarningText: { color: '#b45309' },
  cardMeta: { fontSize: 13, color: '#64748b', marginTop: 4 },
  cardObs: { fontSize: 13, color: '#94a3b8', marginTop: 6, fontStyle: 'italic' },
  actionRow: { flexDirection: 'row', justifyContent: 'flex-start', gap: 16, marginTop: 12, paddingTop: 12, borderTopWidth: 1, borderTopColor: '#e2e8f0' },
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
  areaChip: { borderWidth: 1, borderColor: '#e2e8f0', borderRadius: 20, paddingHorizontal: 14, paddingVertical: 8, marginRight: 8, backgroundColor: '#fff' },
  areaChipActive: { backgroundColor: '#176b87', borderColor: '#176b87' },
  areaChipText: { fontSize: 14, color: '#475569', fontWeight: '600' },
  helpText: { color: '#ef4444', fontSize: 13, marginBottom: 8 },
  switchRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginVertical: 12 },
  saveBtn: { backgroundColor: '#176b87', borderRadius: 12, padding: 16, alignItems: 'center', marginTop: 24, marginBottom: 40 },
  saveBtnText: { color: '#fff', fontSize: 16, fontWeight: '700' },
});
