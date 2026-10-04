import React, { useEffect, useState } from 'react';
import { ActivityIndicator, StyleSheet, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import AppText from './AppText';
import { DemoInfo, getDemoInfo } from '../api/client';
import { theme } from '../theme/theme';

const ROLES: Record<string, { label: string; icon: string }> = {
  sindico: { label: 'síndico', icon: 'shield-checkmark-outline' },
  morador: { label: 'morador', icon: 'home-outline' },
  porteiro: { label: 'porteiro', icon: 'business-outline' },
};

// "Explore a demonstração": aparece só quando a API está em modo demo. As credenciais vêm de GET /api/demo.
export default function DemoAccessCard({ onEnter }: { onEnter: (email: string, password: string) => Promise<void> }) {
  const [demo, setDemo] = useState<DemoInfo | null>(null);
  const [waking, setWaking] = useState(false);
  const [busy, setBusy] = useState('');

  useEffect(() => {
    let active = true;
    const timer = setTimeout(() => active && setWaking(true), 2500);
    getDemoInfo()
      .then(info => { if (active && info?.accounts?.length) setDemo(info); })
      .catch(() => undefined)
      .finally(() => { clearTimeout(timer); if (active) setWaking(false); });
    return () => { active = false; clearTimeout(timer); };
  }, []);

  if (waking) {
    return (
      <View style={[styles.card, styles.row]} accessibilityLiveRegion="polite">
        <ActivityIndicator color={theme.color.brandAccent} />
        <AppText style={styles.hint}>Acordando o servidor de demonstração. A primeira resposta pode levar até 1 minuto.</AppText>
      </View>
    );
  }
  if (!demo) return null;

  return (
    <View style={styles.card} accessibilityRole="summary">
      <AppText style={styles.eyebrow}>DEMONSTRAÇÃO</AppText>
      <AppText style={styles.title} accessibilityRole="header">Explore a demonstração</AppText>
      <AppText style={styles.hint}>Entre com um clique no {demo.condominiumName}.</AppText>
      {demo.accounts.map(account => {
        const role = ROLES[account.role] ?? { label: account.role, icon: 'person-outline' };
        return (
          <TouchableOpacity
            key={account.role}
            accessibilityRole="button"
            accessibilityLabel={`Entrar como ${role.label}`}
            style={styles.role}
            disabled={Boolean(busy)}
            onPress={async () => {
              setBusy(account.role);
              try { await onEnter(account.email, account.password); } finally { setBusy(''); }
            }}
          >
            <Ionicons name={role.icon as never} size={20} color={theme.color.action} />
            <View style={{ flex: 1 }}>
              <AppText style={styles.roleTitle}>{busy === account.role ? 'Entrando...' : `Entrar como ${role.label}`}</AppText>
              <AppText style={styles.roleHint}>{account.name}</AppText>
            </View>
            <Ionicons name="chevron-forward" size={18} color={theme.color.inkMuted} />
          </TouchableOpacity>
        );
      })}
      <AppText style={styles.code}>Código do condomínio para ativar acesso: {demo.condominiumCode}</AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  card: { backgroundColor: theme.color.surface, borderRadius: 16, padding: 16, gap: 8, borderTopWidth: 4, borderTopColor: theme.color.brandAccent, borderWidth: 1, borderColor: theme.color.border },
  row: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  eyebrow: { color: theme.color.accentText, fontSize: 11, fontWeight: '700', letterSpacing: 1 },
  title: { fontSize: 17, fontWeight: '700' },
  hint: { color: theme.color.inkMuted, fontSize: 13, flex: 1 },
  role: { flexDirection: 'row', alignItems: 'center', gap: 12, padding: 12, borderRadius: 10, backgroundColor: theme.color.surfaceSoft, borderWidth: 1, borderColor: theme.color.border, minHeight: 52 },
  roleTitle: { fontSize: 14, fontWeight: '700' },
  roleHint: { fontSize: 12, color: theme.color.inkMuted },
  code: { fontSize: 12, color: theme.color.inkMuted, marginTop: 4 },
});
