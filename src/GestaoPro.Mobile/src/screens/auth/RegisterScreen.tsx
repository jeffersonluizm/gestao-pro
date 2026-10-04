import React, { useEffect, useState } from 'react';
import {
  View, TextInput, TouchableOpacity, StyleSheet,
  KeyboardAvoidingView, Platform, ScrollView, ActivityIndicator, Alert,
} from 'react-native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Ionicons } from '@expo/vector-icons';
import { Picker } from '@react-native-picker/picker';

import { checkCpf, selfRegister, getPublicCondominiums, CondominiumSummary, getFriendlyErrorMessage, getDemoInfo, DemoInfo } from '../../api/client';
import { AuthStackParamList } from '../../navigation/AppNavigator';
import { isValidCpfFormat, maskCpf, maskPhoneBR, onlyDigits } from '../../utils/formatters';
import { isStrongPassword, WEAK_PASSWORD_MESSAGE } from '../../utils/passwordPolicy';

import { theme } from '../../theme/theme';
import AppText from '../../components/AppText';
type Props = { navigation: NativeStackNavigationProp<AuthStackParamList, 'Register'> };

type Step = 'cpf' | 'form' | 'success';

export default function RegisterScreen({ navigation }: Props) {
  const [step, setStep] = useState<Step>('cpf');
  const [loading, setLoading] = useState(false);
  const [condominiums, setCondominiums] = useState<CondominiumSummary[]>([]);

  // Passo 1
  const [cpf, setCpf] = useState('');
  const [condominiumId, setCondominiumId] = useState('');
  // Código do condomínio (senha padrão entregue pela administração): prova o vínculo com o condomínio.
  const [condominiumCode, setCondominiumCode] = useState('');
  // Só com o modo demonstração ligado no servidor: mostra os CPFs pré-cadastrados da demo.
  const [demo, setDemo] = useState<DemoInfo | null>(null);
  useEffect(() => { getDemoInfo().then(setDemo).catch(() => setDemo(null)); }, []);

  // Passo 2
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [needsProfileData, setNeedsProfileData] = useState(true);

  // Sucesso
  const [registeredEmail, setRegisteredEmail] = useState('');

  useEffect(() => {
    getPublicCondominiums()
      .then(setCondominiums)
      .catch(() => Alert.alert('Aviso', 'Não foi possível carregar os condomínios. Verifique a conexão com a API.'));
  }, []);

  async function handleCheckCpf() {
    const rawCpf = onlyDigits(cpf);
    if (!isValidCpfFormat(cpf)) { Alert.alert('Atenção', 'Informe um CPF válido no formato 000.000.000-00.'); return; }
    if (!condominiumId) { Alert.alert('Atenção', 'Selecione um condomínio.'); return; }
    if (!condominiumCode.trim()) { Alert.alert('Atenção', 'Informe o código do condomínio.'); return; }

    setLoading(true);
    try {
      const res = await checkCpf(rawCpf, condominiumId, condominiumCode);
      if (!res.canProceed) {
        Alert.alert('Não foi possível continuar', res.message);
        return;
      }
      setNeedsProfileData(res.needsProfileData);
      setStep('form');
    } catch (err) {
      console.error('Erro ao verificar CPF:', err);
      Alert.alert('Erro', getFriendlyErrorMessage(err, 'Não foi possível verificar o CPF.'));
    } finally {
      setLoading(false);
    }
  }

  async function handleRegister() {
    if (!email.trim()) { Alert.alert('Atenção', 'Informe seu e-mail.'); return; }
    if (!isStrongPassword(password)) { Alert.alert('Atenção', WEAK_PASSWORD_MESSAGE); return; }
    if (password !== confirmPassword) { Alert.alert('Atenção', 'As senhas não coincidem.'); return; }
    if (needsProfileData && !name.trim()) { Alert.alert('Atenção', 'Informe seu nome completo.'); return; }
    if (phone && onlyDigits(phone).length !== 10) { Alert.alert('Atenção', 'Informe um telefone válido no formato 31 9999-2702.'); return; }

    setLoading(true);
    try {
      const res = await selfRegister({
        cpf: onlyDigits(cpf),
        condominiumId,
        condominiumCode,
        password,
        name: needsProfileData ? name.trim() : undefined,
        email: email.trim().toLowerCase(),
        phone: phone.replace(/\D/g, '') || undefined,
      });
      setRegisteredEmail(res.registrationNumber);
      setStep('success');
    } catch (err) {
      console.error('Erro ao criar conta:', err);
      Alert.alert('Erro', getFriendlyErrorMessage(err, 'Não foi possível criar a conta.'));
    } finally {
      setLoading(false);
    }
  }

  // ─── PASSO 1 — Verificar CPF ───────────────────────────────────────────────
  if (step === 'cpf') {
    return (
      <KeyboardAvoidingView style={styles.container} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <ScrollView contentContainerStyle={styles.scroll} keyboardShouldPersistTaps="handled">
          <TouchableOpacity accessibilityRole="button" style={styles.backBtn} onPress={() => navigation.goBack()}>
            <Ionicons name="arrow-back" size={22} color={theme.color.action} />
            <AppText style={styles.backText}>Voltar</AppText>
          </TouchableOpacity>

          <View style={styles.header}>
            <Ionicons name="person-add-outline" size={48} color={theme.color.action} />
            <AppText style={styles.title}>Criar conta</AppText>
            <AppText style={styles.subtitle}>Informe seu CPF para verificarmos o seu cadastro no condomínio.</AppText>
          </View>

          <View style={styles.card}>
            <AppText style={styles.label}>Condomínio</AppText>
            <View style={styles.pickerWrap}>
              <Picker
                selectedValue={condominiumId}
                onValueChange={setCondominiumId}
                style={styles.picker}
              >
                <Picker.Item label="Selecione..." value="" />
                {condominiums.map(c => (
                  <Picker.Item key={c.id} label={c.name} value={c.id} />
                ))}
              </Picker>
            </View>

            <AppText style={styles.label}>CPF</AppText>
            <TextInput accessibilityLabel="CPF"
              style={styles.input}
              placeholder="000.000.000-00"
              keyboardType="numeric"
              value={cpf}
              onChangeText={v => setCpf(maskCpf(v))}
              maxLength={14}
            />

            <AppText style={styles.label}>Código do condomínio</AppText>
            <TextInput accessibilityLabel="Código do condomínio"
              style={styles.input}
              placeholder="Informado pela administração"
              secureTextEntry
              autoCapitalize="none"
              value={condominiumCode}
              onChangeText={setCondominiumCode}
            />

            <TouchableOpacity accessibilityRole="button" style={styles.button} onPress={handleCheckCpf} disabled={loading}>
              {loading ? <ActivityIndicator color="#fff" /> : <AppText style={styles.buttonText}>Verificar CPF</AppText>}
            </TouchableOpacity>
          </View>

          {demo && (
            <View style={styles.hintCard}>
              <AppText style={styles.hintTitle}>Demonstração: CPFs pré-cadastrados</AppText>
              {demo.preRegistered.map(resident => (
                <AppText key={resident.cpf} style={styles.hintLine}>{resident.cpf} · {resident.name}</AppText>
              ))}
              <AppText style={styles.hintNote}>Condomínio: {demo.condominiumName} · código {demo.condominiumCode}</AppText>
            </View>
          )}
        </ScrollView>
      </KeyboardAvoidingView>
    );
  }

  // ─── PASSO 2 — Preencher dados ─────────────────────────────────────────────
  if (step === 'form') {
    return (
      <KeyboardAvoidingView style={styles.container} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <ScrollView contentContainerStyle={styles.scroll} keyboardShouldPersistTaps="handled">
          <TouchableOpacity accessibilityRole="button" style={styles.backBtn} onPress={() => setStep('cpf')}>
            <Ionicons name="arrow-back" size={22} color={theme.color.action} />
            <AppText style={styles.backText}>Voltar</AppText>
          </TouchableOpacity>

          <View style={styles.header}>
            <Ionicons name="create-outline" size={48} color={theme.color.action} />
            <AppText style={styles.title}>Complete seu cadastro</AppText>
            <AppText style={styles.subtitle}>CPF verificado! Preencha seus dados para criar a conta.</AppText>
          </View>

          <View style={styles.card}>
            {needsProfileData && (
              <>
                <AppText style={styles.label}>Nome completo *</AppText>
                <TextInput accessibilityLabel="Nome completo"
                  style={styles.input}
                  placeholder="Seu nome completo"
                  value={name}
                  onChangeText={setName}
                  autoCapitalize="words"
                />
              </>
            )}

            <AppText style={styles.label}>E-mail *</AppText>
            <TextInput accessibilityLabel="E-mail"
              style={styles.input}
              placeholder="seu@email.com"
              keyboardType="email-address"
              autoCapitalize="none"
              value={email}
              onChangeText={setEmail}
            />

            <AppText style={styles.label}>Telefone</AppText>
            <TextInput accessibilityLabel="Telefone"
              style={styles.input}
              placeholder="31 9999-2702"
              keyboardType="phone-pad"
              value={phone}
              onChangeText={value => setPhone(maskPhoneBR(value))}
              maxLength={12}
            />

            <AppText style={styles.label}>Senha *</AppText>
            <TextInput accessibilityLabel="Senha"
              style={styles.input}
              placeholder="Mínimo 8 caracteres"
              secureTextEntry
              value={password}
              onChangeText={setPassword}
            />

            <AppText style={styles.label}>Confirmar senha *</AppText>
            <TextInput accessibilityLabel="Confirmar senha"
              style={styles.input}
              placeholder="Repita a senha"
              secureTextEntry
              value={confirmPassword}
              onChangeText={setConfirmPassword}
            />

            <TouchableOpacity accessibilityRole="button" style={styles.button} onPress={handleRegister} disabled={loading}>
              {loading ? <ActivityIndicator color="#fff" /> : <AppText style={styles.buttonText}>Criar conta</AppText>}
            </TouchableOpacity>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    );
  }

  // ─── SUCESSO ───────────────────────────────────────────────────────────────
  return (
    <View style={[styles.container, styles.successContainer]}>
      <View style={styles.successCard}>
        <Ionicons name="checkmark-circle" size={72} color={theme.color.success} style={{ marginBottom: 16 }} />
        <AppText style={styles.successTitle}>Conta criada!</AppText>
        <AppText style={styles.successText}>Seu acesso foi configurado. Faça login com:</AppText>
        <View style={styles.successEmailBox}>
          <AppText style={styles.successEmail}>{registeredEmail}</AppText>
        </View>
        <TouchableOpacity accessibilityRole="button" style={[styles.button, { marginTop: 24 }]} onPress={() => navigation.navigate('Login')}>
          <AppText style={styles.buttonText}>Ir para Login</AppText>
        </TouchableOpacity>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.color.background },
  scroll: { flexGrow: 1, padding: 24, paddingTop: 56 },
  backBtn: { flexDirection: 'row', alignItems: 'center', marginBottom: 24 },
  backText: { color: theme.color.action, fontSize: 15, marginLeft: 6, fontWeight: '600' },
  header: { alignItems: 'center', marginBottom: 28 },
  title: { fontSize: 22, fontWeight: '700', color: theme.color.ink, marginTop: 12 },
  subtitle: { fontSize: 14, color: theme.color.inkMuted, marginTop: 6, textAlign: 'center' },
  card: {
    backgroundColor: '#fff', borderRadius: 16, padding: 24,
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08, shadowRadius: 8, elevation: 3,
  },
  label: { fontSize: 13, fontWeight: '600', color: theme.color.inkMuted, marginBottom: 4, marginTop: 12 },
  input: {
    borderWidth: 1, borderColor: theme.color.border, borderRadius: 10,
    padding: 12, fontSize: 15, backgroundColor: theme.color.background, marginBottom: 4,
  },
  pickerWrap: {
    borderWidth: 1, borderColor: theme.color.border, borderRadius: 10,
    backgroundColor: theme.color.background, marginBottom: 4, overflow: 'hidden',
  },
  picker: { height: 50 },
  button: {
    backgroundColor: theme.color.action, borderRadius: 10, padding: 14,
    alignItems: 'center', marginTop: 16,
  },
  buttonText: { color: '#fff', fontSize: 16, fontWeight: '700' },
  hintCard: {
    backgroundColor: theme.color.successSoft, borderWidth: 1, borderColor: theme.color.successSoft,
    borderRadius: 12, padding: 14, marginTop: 20,
  },
  hintTitle: { fontSize: 13, fontWeight: '700', color: theme.color.success, marginBottom: 6 },
  hintLine: { fontSize: 12, color: theme.color.success, marginBottom: 2 },
  hintNote: { fontSize: 12, color: theme.color.success, marginTop: 4, fontStyle: 'italic' },
  successContainer: { justifyContent: 'center', alignItems: 'center', padding: 24 },
  successCard: {
    backgroundColor: '#fff', borderRadius: 20, padding: 32, alignItems: 'center',
    shadowColor: '#000', shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1, shadowRadius: 12, elevation: 5, width: '100%',
  },
  successTitle: { fontSize: 26, fontWeight: '700', color: theme.color.ink, marginBottom: 8 },
  successText: { fontSize: 15, color: theme.color.inkMuted, textAlign: 'center', marginBottom: 16 },
  successEmailBox: {
    backgroundColor: theme.color.actionSoft, borderWidth: 1, borderColor: theme.color.border,
    borderRadius: 10, padding: 12, width: '100%', alignItems: 'center',
  },
  successEmail: { fontSize: 16, fontWeight: '700', color: theme.color.actionHover },
});
