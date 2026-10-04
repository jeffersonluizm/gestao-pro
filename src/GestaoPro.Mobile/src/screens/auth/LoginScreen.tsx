import React, { useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, StyleSheet,
  KeyboardAvoidingView, Platform, ScrollView, ActivityIndicator, Alert,
} from 'react-native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Ionicons } from '@expo/vector-icons';

import { login, changePasswordOnFirstLogin, getFriendlyErrorMessage } from '../../api/client';
import { isStrongPassword, WEAK_PASSWORD_MESSAGE } from '../../utils/passwordPolicy';
import { useAuth } from '../../context/AuthContext';
import { AuthStackParamList } from '../../navigation/AppNavigator';
import { sw, sf, HP } from '../../utils/responsive';

type Props = { navigation: NativeStackNavigationProp<AuthStackParamList, 'Login'> };

export default function LoginScreen({ navigation }: Props) {
  const { signIn } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPwd, setShowPwd] = useState(false);
  const [loading, setLoading] = useState(false);

  // Troca de senha obrigatória no primeiro acesso
  const [mustChange, setMustChange] = useState(false);
  const [pendingUserId, setPendingUserId] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  async function handleLogin() {
    if (!email.trim() || !password.trim()) {
      Alert.alert('Atenção', 'Preencha e-mail e senha.');
      return;
    }
    setLoading(true);
    try {
      const res = await login(email.trim().toLowerCase(), password);
      if (res.mustChangePassword) {
        setPendingUserId(res.userId);
        setMustChange(true);
        return;
      }
      await signIn({
        token: res.token,
        userId: res.userId,
        userName: res.userName,
        role: res.role,
        condominiumId: res.condominiumId,
        registrationNumber: res.registrationNumber,
      });
    } catch (err: any) {
      console.error('Erro ao fazer login:', err);
      Alert.alert('Erro', getFriendlyErrorMessage(err, 'Falha ao fazer login.'));
    } finally {
      setLoading(false);
    }
  }

  async function handleChangePassword() {
    if (!isStrongPassword(newPassword)) { Alert.alert('Atenção', WEAK_PASSWORD_MESSAGE); return; }
    if (newPassword !== confirmPassword) { Alert.alert('Atenção', 'As senhas não coincidem.'); return; }
    setLoading(true);
    try {
      await changePasswordOnFirstLogin(pendingUserId, password, newPassword);
      setMustChange(false);
      setPassword(newPassword);
      setNewPassword('');
      setConfirmPassword('');
      Alert.alert('Sucesso', 'Senha alterada. Faça login novamente.', [
        { text: 'OK', onPress: handleLogin },
      ]);
    } catch (err: any) {
      console.error('Erro ao alterar senha:', err);
      Alert.alert('Erro', getFriendlyErrorMessage(err, 'Não foi possível alterar a senha.'));
    } finally {
      setLoading(false);
    }
  }

  if (mustChange) {
    return (
      <KeyboardAvoidingView style={styles.container} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <View style={styles.card}>
          <Ionicons name="lock-closed-outline" size={48} color="#176b87" style={styles.centerIcon} />
          <Text style={styles.title}>Primeiro acesso</Text>
          <Text style={styles.subtitle}>Crie uma nova senha para continuar.</Text>
          <TextInput style={styles.input} placeholder="Nova senha" secureTextEntry value={newPassword} onChangeText={setNewPassword} />
          <TextInput style={styles.input} placeholder="Confirmar nova senha" secureTextEntry value={confirmPassword} onChangeText={setConfirmPassword} />
          <TouchableOpacity style={styles.button} onPress={handleChangePassword} disabled={loading}>
            {loading ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>Salvar senha</Text>}
          </TouchableOpacity>
        </View>
      </KeyboardAvoidingView>
    );
  }

  return (
    <KeyboardAvoidingView style={styles.container} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView contentContainerStyle={styles.scroll} keyboardShouldPersistTaps="handled">
        {/* Header */}
        <View style={styles.header}>
          <View style={styles.logoCircle}>
            <Ionicons name="business" size={40} color="#fff" />
          </View>
          <Text style={styles.appName}>GestaoPro</Text>
          <Text style={styles.appTagline}>Eficiência para seu condomínio</Text>
        </View>

        {/* Card */}
        <View style={styles.card}>
          <Text style={styles.title}>Entrar</Text>

          <Text style={styles.label}>E-mail</Text>
          <TextInput
            style={styles.input}
            placeholder="seu@email.com"
            keyboardType="email-address"
            autoCapitalize="none"
            value={email}
            onChangeText={setEmail}
          />

          <Text style={styles.label}>Senha</Text>
          <View style={styles.passwordRow}>
            <TextInput
              style={[styles.input, { flex: 1, marginBottom: 0 }]}
              placeholder="••••••••"
              secureTextEntry={!showPwd}
              value={password}
              onChangeText={setPassword}
            />
            <TouchableOpacity onPress={() => setShowPwd(v => !v)} style={styles.eyeBtn}>
              <Ionicons name={showPwd ? 'eye-off-outline' : 'eye-outline'} size={20} color="#64748b" />
            </TouchableOpacity>
          </View>

          <TouchableOpacity style={styles.button} onPress={handleLogin} disabled={loading}>
            {loading ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>Entrar</Text>}
          </TouchableOpacity>
        </View>

        {/* Link para cadastro */}
        <View style={styles.registerRow}>
          <Text style={styles.registerText}>Primeira vez? </Text>
          <TouchableOpacity onPress={() => navigation.navigate('Register')}>
            <Text style={styles.registerLink}>Criar conta</Text>
          </TouchableOpacity>
        </View>

        {/* Credenciais de teste: a API só cria essas contas em desenvolvimento */}
        {__DEV__ && (
          <View style={styles.testCard}>
            <Text style={styles.testTitle}>🧪 Credenciais de teste</Text>
            <Text style={styles.testLine}>Admin → admin@gestaopro.local / Admin@123</Text>
            <Text style={styles.testLine}>CPFs mock para cadastro:</Text>
            <Text style={styles.testLine}>  111.111.111-11 (João da Silva)</Text>
            <Text style={styles.testLine}>  222.222.222-22 (Maria Santos)</Text>
            <Text style={styles.testLine}>  333.333.333-33 (Pedro Oliveira)</Text>
          </View>
        )}
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f0f4ff' },
  scroll: { flexGrow: 1, paddingHorizontal: HP, paddingTop: sw(60), paddingBottom: sw(24) },
  header: { alignItems: 'center', marginBottom: sw(32) },
  logoCircle: {
    width: sw(80), height: sw(80), borderRadius: sw(40),
    backgroundColor: '#176b87', justifyContent: 'center', alignItems: 'center',
    marginBottom: sw(12),
  },
  appName: { fontSize: sf(26), fontWeight: '700', color: '#1e293b' },
  appTagline: { fontSize: sf(14), color: '#64748b', marginTop: sw(4) },
  card: {
    backgroundColor: '#fff', borderRadius: sw(16), padding: sw(24),
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08, shadowRadius: 8, elevation: 3,
    marginBottom: sw(20),
  },
  centerIcon: { alignSelf: 'center', marginBottom: sw(12) },
  title: { fontSize: sf(22), fontWeight: '700', color: '#1e293b', marginBottom: sw(4) },
  subtitle: { fontSize: sf(14), color: '#64748b', marginBottom: sw(20) },
  label: { fontSize: sf(13), fontWeight: '600', color: '#475569', marginBottom: sw(4), marginTop: sw(12) },
  input: {
    borderWidth: 1, borderColor: '#e2e8f0', borderRadius: sw(10),
    padding: sw(12), fontSize: sf(15), backgroundColor: '#f8fafc', marginBottom: sw(12),
  },
  passwordRow: { flexDirection: 'row', alignItems: 'center', marginBottom: sw(20) },
  eyeBtn: { padding: sw(12) },
  button: {
    backgroundColor: '#176b87', borderRadius: sw(10), padding: sw(14),
    alignItems: 'center', marginTop: sw(4),
  },
  buttonText: { color: '#fff', fontSize: sf(16), fontWeight: '700' },
  registerRow: { flexDirection: 'row', justifyContent: 'center', marginBottom: sw(24) },
  registerText: { color: '#64748b', fontSize: sf(14) },
  registerLink: { color: '#176b87', fontSize: sf(14), fontWeight: '600' },
  testCard: {
    backgroundColor: '#fefce8', borderWidth: 1, borderColor: '#fde047',
    borderRadius: sw(12), padding: sw(14),
  },
  testTitle: { fontSize: sf(13), fontWeight: '700', color: '#854d0e', marginBottom: sw(6) },
  testLine: { fontSize: sf(12), color: '#713f12', marginBottom: sw(2) },
});
