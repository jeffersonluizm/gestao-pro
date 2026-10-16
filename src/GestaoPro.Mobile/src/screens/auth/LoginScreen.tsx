import React, { useState } from 'react';
import {
  View, Image, TextInput, TouchableOpacity, StyleSheet,
  KeyboardAvoidingView, Platform, ScrollView, ActivityIndicator, Alert,
} from 'react-native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Ionicons } from '@expo/vector-icons';

import { login, changePasswordOnFirstLogin, enterDemo, getFriendlyErrorMessage } from '../../api/client';
import { enterDemoWithRetry, forgetDemoRole, rememberDemoRole } from '../../utils/demoSession';
import { isStrongPassword, WEAK_PASSWORD_MESSAGE } from '../../utils/passwordPolicy';
import { useAuth } from '../../context/AuthContext';
import { AuthStackParamList } from '../../navigation/AppNavigator';
import { sw, sf, HP } from '../../utils/responsive';

import { theme } from '../../theme/theme';
import AppText from '../../components/AppText';
import DemoAccessCard from '../../components/DemoAccessCard';
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

  // Entrada com um clique pela demonstração: a API devolve a sessão do papel, sem senha.
  async function enterWith(role: string) {
    try {
      const res = await enterDemoWithRetry(enterDemo, role);
      rememberDemoRole(role);
      await signIn({
        token: res.token, userId: res.userId, userName: res.userName, role: res.role,
        condominiumId: res.condominiumId, registrationNumber: res.registrationNumber,
      });
    } catch (err) {
      Alert.alert('Erro', getFriendlyErrorMessage(err, 'Não foi possível entrar na demonstração.'));
    }
  }

  async function handleLogin() {
    if (!email.trim() || !password.trim()) {
      Alert.alert('Atenção', 'Preencha e-mail e senha.');
      return;
    }
    setLoading(true);
    try {
      const res = await login(email.trim().toLowerCase(), password);
      forgetDemoRole();
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
    } catch (err) {
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
    } catch (err) {
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
          <Ionicons name="lock-closed-outline" size={48} color={theme.color.action} style={styles.centerIcon} />
          <AppText style={styles.title}>Primeiro acesso</AppText>
          <AppText style={styles.subtitle}>Crie uma nova senha para continuar.</AppText>
          <AppText style={styles.label}>Nova senha</AppText>
          <TextInput style={styles.input} accessibilityLabel="Nova senha" placeholder="Mínimo 8 caracteres" secureTextEntry value={newPassword} onChangeText={setNewPassword} />
          <AppText style={styles.label}>Confirmar nova senha</AppText>
          <TextInput style={styles.input} accessibilityLabel="Confirmar nova senha" placeholder="Repita a senha" secureTextEntry value={confirmPassword} onChangeText={setConfirmPassword} />
          <TouchableOpacity accessibilityRole="button" style={styles.button} onPress={handleChangePassword} disabled={loading}>
            {loading ? <ActivityIndicator color="#fff" /> : <AppText style={styles.buttonText}>Salvar senha</AppText>}
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
          <Image source={require('../../../assets/logo.png')} style={styles.logo} accessible accessibilityLabel="Logo do Gestão Pro" />
          <AppText style={styles.appName} accessibilityRole="header">Gestão Pro</AppText>
          <AppText style={styles.appTagline}>Eficiência para o seu condomínio</AppText>
        </View>

        {/* Card */}
        <View style={styles.card}>
          <AppText style={styles.title}>Entrar</AppText>

          <AppText style={styles.label}>E-mail</AppText>
          <TextInput
            style={styles.input}
            placeholder="seu@email.com"
            accessibilityLabel="E-mail"
            autoComplete="email"
            keyboardType="email-address"
            autoCapitalize="none"
            value={email}
            onChangeText={setEmail}
          />

          <AppText style={styles.label}>Senha</AppText>
          <View style={styles.passwordRow}>
            <TextInput
              style={[styles.input, { flex: 1, marginBottom: 0 }]}
              placeholder="Sua senha"
              accessibilityLabel="Senha"
              autoComplete="password"
              secureTextEntry={!showPwd}
              value={password}
              onChangeText={setPassword}
            />
            <TouchableOpacity accessibilityRole="button" accessibilityLabel={showPwd ? 'Ocultar senha' : 'Mostrar senha'} onPress={() => setShowPwd(v => !v)} style={styles.eyeBtn}>
              <Ionicons name={showPwd ? 'eye-off-outline' : 'eye-outline'} size={20} color={theme.color.inkMuted} />
            </TouchableOpacity>
          </View>

          <TouchableOpacity accessibilityRole="button" style={styles.button} onPress={handleLogin} disabled={loading}>
            {loading ? <ActivityIndicator color="#fff" /> : <AppText style={styles.buttonText}>Entrar</AppText>}
          </TouchableOpacity>
        </View>

        {/* Link para cadastro */}
        <View style={styles.registerRow}>
          <AppText style={styles.registerText}>Primeira vez? </AppText>
          <TouchableOpacity accessibilityRole="button" onPress={() => navigation.navigate('Register')}>
            <AppText style={styles.registerLink}>Ative o seu acesso</AppText>
          </TouchableOpacity>
        </View>

        {/* Demonstração: só com o modo demo ligado no servidor (credenciais vêm da API) */}
        <DemoAccessCard onEnter={enterWith} />
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.color.background },
  scroll: { flexGrow: 1, paddingHorizontal: HP, paddingTop: sw(60), paddingBottom: sw(24) },
  header: { alignItems: 'center', marginBottom: sw(32) },
  logo: { width: sw(84), height: sw(84), borderRadius: sw(18), marginBottom: sw(12) },
  appName: { fontSize: sf(26), fontWeight: '700', color: theme.color.ink },
  appTagline: { fontSize: sf(14), color: theme.color.inkMuted, marginTop: sw(4) },
  card: {
    backgroundColor: '#fff', borderRadius: sw(16), padding: sw(24),
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08, shadowRadius: 8, elevation: 3,
    marginBottom: sw(20),
  },
  centerIcon: { alignSelf: 'center', marginBottom: sw(12) },
  title: { fontSize: sf(22), fontWeight: '700', color: theme.color.ink, marginBottom: sw(4) },
  subtitle: { fontSize: sf(14), color: theme.color.inkMuted, marginBottom: sw(20) },
  label: { fontSize: sf(13), fontWeight: '600', color: theme.color.inkMuted, marginBottom: sw(4), marginTop: sw(12) },
  input: {
    borderWidth: 1, borderColor: theme.color.border, borderRadius: sw(10),
    padding: sw(12), fontSize: sf(15), backgroundColor: theme.color.background, marginBottom: sw(12),
  },
  passwordRow: { flexDirection: 'row', alignItems: 'center', marginBottom: sw(20) },
  eyeBtn: { padding: sw(12) },
  button: {
    backgroundColor: theme.color.action, borderRadius: sw(10), padding: sw(14),
    alignItems: 'center', marginTop: sw(4),
  },
  buttonText: { color: '#fff', fontSize: sf(16), fontWeight: '700' },
  registerRow: { flexDirection: 'row', justifyContent: 'center', marginBottom: sw(24) },
  registerText: { color: theme.color.inkMuted, fontSize: sf(14) },
  registerLink: { color: theme.color.action, fontSize: sf(14), fontWeight: '600' },
});
