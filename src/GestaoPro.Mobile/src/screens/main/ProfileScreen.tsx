import React, { useCallback, useState } from 'react';
import {
  View, Text, StyleSheet, TouchableOpacity, Alert,
  ActivityIndicator, ScrollView, Platform, Modal, TextInput,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';

import { getUserById, changeUserPassword, User, getFriendlyErrorMessage } from '../../api/client';
import { isStrongPassword, WEAK_PASSWORD_MESSAGE } from '../../utils/passwordPolicy';
import { useAuth } from '../../context/AuthContext';
import { sw, sf, HP } from '../../utils/responsive';

const ROLE_LABEL: Record<string, string> = {
  admin: 'Administrador',
  sindico: 'Síndico',
  morador: 'Morador',
  porteiro: 'Porteiro',
};

export default function ProfileScreen() {
  const { session, signOut } = useAuth();
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  // Modal de alterar senha
  const [pwdModal, setPwdModal] = useState(false);
  const [currentPwd, setCurrentPwd] = useState('');
  const [newPwd, setNewPwd] = useState('');
  const [confirmPwd, setConfirmPwd] = useState('');
  const [pwdError, setPwdError] = useState('');
  const [savingPwd, setSavingPwd] = useState(false);

  async function handleChangePassword() {
    setPwdError('');
    if (!currentPwd || !newPwd || !confirmPwd) {
      setPwdError('Preencha todos os campos.');
      return;
    }
    if (!isStrongPassword(newPwd)) {
      setPwdError(WEAK_PASSWORD_MESSAGE);
      return;
    }
    if (newPwd !== confirmPwd) {
      setPwdError('A nova senha e a confirmação não coincidem.');
      return;
    }
    setSavingPwd(true);
    try {
      await changeUserPassword(session!.userId, currentPwd, newPwd);
      setPwdModal(false);
      setCurrentPwd(''); setNewPwd(''); setConfirmPwd('');
      Alert.alert('Sucesso', 'Senha alterada com sucesso.');
    } catch (e: any) {
      console.error('Erro ao alterar senha:', e);
      setPwdError(getFriendlyErrorMessage(e, 'Não foi possível alterar a senha.'));
    } finally {
      setSavingPwd(false);
    }
  }

  function openPwdModal() {
    setCurrentPwd(''); setNewPwd(''); setConfirmPwd(''); setPwdError('');
    setPwdModal(true);
  }


  async function load() {
    setLoading(true);
    try {
      // Timeout de 5s para não travar o perfil quando a API está offline
      const timeoutPromise = new Promise<null>((_, reject) =>
        setTimeout(() => reject(new Error('timeout')), 5000)
      );
      const data = await Promise.race([getUserById(session!.userId), timeoutPromise]);
      if (data) setUser(data as any);
    } catch {
      // Usa os dados da sessão como fallback
    } finally {
      setLoading(false);
    }
  }

  useFocusEffect(useCallback(() => { load(); }, []));

  function handleLogout() {
    if (Platform.OS === 'web') {
      // window.confirm is more reliable than Alert.alert on web
      if (window.confirm('Tem certeza que deseja encerrar a sessão?')) {
        signOut();
      }
    } else {
      Alert.alert(
        'Sair',
        'Tem certeza que deseja encerrar a sessão?',
        [
          { text: 'Cancelar', style: 'cancel' },
          { text: 'Sair', style: 'destructive', onPress: signOut },
        ],
      );
    }
  }

  const displayName = user?.name || session?.userName || 'Usuário';
  const displayEmail = user?.email || '';
  const displayRole = ROLE_LABEL[user?.role ?? session?.role ?? ''] ?? (session?.role ?? '');
  const initials = displayName
    .split(' ')
    .slice(0, 2)
    .map(w => w[0]?.toUpperCase() ?? '')
    .join('');

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Perfil</Text>
      </View>

      {loading ? (
        <ActivityIndicator size="large" color="#176b87" style={{ marginTop: sw(60) }} />
      ) : (
        <ScrollView contentContainerStyle={styles.scroll}>
          {/* Avatar + nome */}
          <View style={styles.avatarSection}>
            <View style={styles.avatar}>
              <Text style={styles.avatarText}>{initials || '?'}</Text>
            </View>
            <Text style={styles.displayName}>{displayName}</Text>
            <View style={styles.roleBadge}>
              <Text style={styles.roleText}>{displayRole}</Text>
            </View>
          </View>

          {/* Informações */}
          <View style={styles.card}>
            <Text style={styles.sectionTitle}>Informações</Text>

            <InfoRow icon="mail-outline" label="E-mail" value={displayEmail || '—'} />
            <InfoRow icon="call-outline" label="Telefone" value={user?.phone || '—'} />
            <InfoRow
              icon="card-outline"
              label="CPF"
              value={user?.cpf ? maskCpf(user.cpf) : '—'}
            />
            <InfoRow
              icon="home-outline"
              label="Matrícula / E-mail"
              value={user?.registrationNumber || session?.registrationNumber || '—'}
            />
          </View>

          {/* Ações */}
          <View style={styles.card}>
            <Text style={styles.sectionTitle}>Conta</Text>

            <TouchableOpacity style={styles.actionRow} onPress={openPwdModal}>
              <View style={styles.actionLeft}>
                <View style={[styles.actionIcon, { backgroundColor: '#eff6ff' }]}>
                  <Ionicons name="lock-closed-outline" size={sw(18)} color="#176b87" />
                </View>
                <Text style={styles.actionLabel}>Alterar senha</Text>
              </View>
              <Ionicons name="chevron-forward" size={sw(18)} color="#94a3b8" />
            </TouchableOpacity>

            <View style={styles.divider} />

            <TouchableOpacity style={styles.actionRow} onPress={handleLogout}>
              <View style={styles.actionLeft}>
                <View style={[styles.actionIcon, { backgroundColor: '#fef2f2' }]}>
                  <Ionicons name="log-out-outline" size={sw(18)} color="#ef4444" />
                </View>
                <Text style={[styles.actionLabel, { color: '#ef4444' }]}>Sair da conta</Text>
              </View>
              <Ionicons name="chevron-forward" size={sw(18)} color="#94a3b8" />
            </TouchableOpacity>
          </View>

          {/* Versão */}
          <Text style={styles.version}>GestaoPro Mobile v1.0.0</Text>
        </ScrollView>
      )}

      {/* Modal — Alterar Senha */}
      <Modal visible={pwdModal} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={pwdStyles.container}>
          <View style={pwdStyles.header}>
            <Text style={pwdStyles.title}>Alterar Senha</Text>
            <TouchableOpacity onPress={() => setPwdModal(false)}>
              <Ionicons name="close" size={sw(26)} color="#64748b" />
            </TouchableOpacity>
          </View>

          <ScrollView style={pwdStyles.body} keyboardShouldPersistTaps="handled">
            {pwdError ? (
              <View style={pwdStyles.errorBox}>
                <Ionicons name="alert-circle-outline" size={sw(16)} color="#dc2626" />
                <Text style={pwdStyles.errorText}>{pwdError}</Text>
              </View>
            ) : null}

            <Text style={pwdStyles.label}>Senha atual *</Text>
            <TextInput
              style={pwdStyles.input}
              secureTextEntry
              placeholder="••••••••"
              value={currentPwd}
              onChangeText={setCurrentPwd}
            />

            <Text style={pwdStyles.label}>Nova senha *</Text>
            <TextInput
              style={pwdStyles.input}
              secureTextEntry
              placeholder="Mínimo 8 caracteres"
              value={newPwd}
              onChangeText={setNewPwd}
            />

            <Text style={pwdStyles.label}>Confirmar nova senha *</Text>
            <TextInput
              style={pwdStyles.input}
              secureTextEntry
              placeholder="••••••••"
              value={confirmPwd}
              onChangeText={setConfirmPwd}
            />

            <TouchableOpacity style={pwdStyles.btn} onPress={handleChangePassword} disabled={savingPwd}>
              {savingPwd
                ? <ActivityIndicator color="#fff" />
                : <Text style={pwdStyles.btnText}>Salvar nova senha</Text>}
            </TouchableOpacity>
          </ScrollView>
        </SafeAreaView>
      </Modal>
    </SafeAreaView>
  );
}

function InfoRow({ icon, label, value }: { icon: any; label: string; value: string }) {
  return (
    <View style={infoStyles.row}>
      <View style={infoStyles.iconWrap}>
        <Ionicons name={icon} size={sw(18)} color="#64748b" />
      </View>
      <View style={infoStyles.textWrap}>
        <Text style={infoStyles.label}>{label}</Text>
        <Text style={infoStyles.value}>{value}</Text>
      </View>
    </View>
  );
}

function maskCpf(cpf: string): string {
  const d = cpf.replace(/\D/g, '');
  if (d.length !== 11) return cpf;
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.***-**`;
}

const infoStyles = StyleSheet.create({
  row: { flexDirection: 'row', alignItems: 'center', paddingVertical: sw(10) },
  iconWrap: { width: sw(32), alignItems: 'center' },
  textWrap: { flex: 1, marginLeft: sw(8) },
  label: { fontSize: sf(11), color: '#94a3b8', fontWeight: '600', textTransform: 'uppercase', marginBottom: sw(1) },
  value: { fontSize: sf(15), color: '#1e293b' },
});

const pwdStyles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  header: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    padding: HP, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0',
  },
  title: { fontSize: sf(18), fontWeight: '700', color: '#1e293b' },
  body: { padding: HP },
  errorBox: {
    flexDirection: 'row', alignItems: 'center', gap: sw(6),
    backgroundColor: '#fef2f2', borderRadius: sw(8), padding: sw(12), marginBottom: sw(12),
  },
  errorText: { color: '#dc2626', fontSize: sf(13), flex: 1 },
  label: { fontSize: sf(13), fontWeight: '600', color: '#475569', marginBottom: sw(6), marginTop: sw(14) },
  input: {
    borderWidth: 1, borderColor: '#e2e8f0', borderRadius: sw(10),
    padding: sw(12), fontSize: sf(15), backgroundColor: '#fff',
  },
  btn: {
    backgroundColor: '#176b87', borderRadius: sw(12),
    padding: sw(16), alignItems: 'center', marginTop: sw(28), marginBottom: sw(40),
  },
  btnText: { color: '#fff', fontSize: sf(16), fontWeight: '700' },
});

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  header: {
    paddingHorizontal: HP, paddingVertical: sw(16),
    backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0',
  },
  title: { fontSize: sf(20), fontWeight: '700', color: '#1e293b' },
  scroll: { padding: HP, paddingBottom: sw(40) },
  avatarSection: { alignItems: 'center', paddingVertical: sw(28) },
  avatar: {
    width: sw(80), height: sw(80), borderRadius: sw(40),
    backgroundColor: '#176b87', justifyContent: 'center', alignItems: 'center',
    marginBottom: sw(12),
    shadowColor: '#176b87', shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3, shadowRadius: 8, elevation: 6,
  },
  avatarText: { fontSize: sf(28), fontWeight: '700', color: '#fff' },
  displayName: { fontSize: sf(20), fontWeight: '700', color: '#1e293b', marginBottom: sw(6) },
  roleBadge: {
    backgroundColor: '#eff6ff', borderRadius: sw(20), paddingHorizontal: sw(14), paddingVertical: sw(4),
  },
  roleText: { fontSize: sf(13), color: '#176b87', fontWeight: '600' },
  card: {
    backgroundColor: '#fff', borderRadius: sw(16), padding: HP, marginBottom: sw(16),
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06, shadowRadius: 4, elevation: 2,
  },
  sectionTitle: {
    fontSize: sf(12), fontWeight: '700', color: '#94a3b8',
    textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: sw(8),
  },
  divider: { height: 1, backgroundColor: '#f1f5f9', marginVertical: sw(4) },
  actionRow: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingVertical: sw(12),
  },
  actionLeft: { flexDirection: 'row', alignItems: 'center', gap: sw(12) },
  actionIcon: { width: sw(36), height: sw(36), borderRadius: sw(10), justifyContent: 'center', alignItems: 'center' },
  actionLabel: { fontSize: sf(15), color: '#1e293b', fontWeight: '500' },
  version: { textAlign: 'center', fontSize: sf(12), color: '#cbd5e1', marginTop: sw(8) },
});
