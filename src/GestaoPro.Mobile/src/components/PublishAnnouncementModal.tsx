import React, { useState } from 'react';
import { ActivityIndicator, Modal, ScrollView, StyleSheet, TextInput, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { createAnnouncement, getFriendlyErrorMessage } from '../api/client';
import AppText from './AppText';
import { theme } from '../theme/theme';

const CATEGORIAS = ['Geral', 'Manutenção', 'Segurança', 'Evento', 'Financeiro', 'Assembleia'];

// Síndico e administração publicam aviso pelo app (o condomínio é o da sessão).
export default function PublishAnnouncementModal({
  visible, condominiumId, authorId, onClose, onPublished,
}: {
  visible: boolean;
  condominiumId: string;
  authorId: string;
  onClose: () => void;
  onPublished: () => void;
}) {
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [category, setCategory] = useState('Geral');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  async function publish() {
    if (!title.trim() || !content.trim()) {
      setError('Informe o título e o texto do aviso.');
      return;
    }
    setSaving(true);
    setError('');
    try {
      await createAnnouncement({ condominiumId, authorId, title: title.trim(), content: content.trim(), category });
      setTitle('');
      setContent('');
      setCategory('Geral');
      onPublished();
    } catch (err) {
      setError(getFriendlyErrorMessage(err, 'Não foi possível publicar o aviso.'));
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal visible={visible} animationType="slide" onRequestClose={onClose}>
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <AppText style={styles.title} accessibilityRole="header">Publicar aviso</AppText>
          <TouchableOpacity accessibilityRole="button" accessibilityLabel="Fechar" onPress={onClose} hitSlop={12}>
            <AppText style={styles.close}>Fechar</AppText>
          </TouchableOpacity>
        </View>
        <ScrollView contentContainerStyle={styles.body} keyboardShouldPersistTaps="handled">
          <AppText style={styles.label}>Título</AppText>
          <TextInput style={styles.input} accessibilityLabel="Título" value={title} onChangeText={setTitle} maxLength={120} />
          <AppText style={styles.label}>Categoria</AppText>
          <View style={styles.chips} accessibilityRole="radiogroup">
            {CATEGORIAS.map(item => (
              <TouchableOpacity
                key={item}
                accessibilityRole="radio"
                accessibilityState={{ checked: category === item }}
                style={[styles.chip, category === item && styles.chipActive]}
                onPress={() => setCategory(item)}
              >
                <AppText style={[styles.chipText, category === item && styles.chipTextActive]}>{item}</AppText>
              </TouchableOpacity>
            ))}
          </View>
          <AppText style={styles.label}>Texto</AppText>
          <TextInput style={[styles.input, styles.textarea]} accessibilityLabel="Texto do aviso" value={content} onChangeText={setContent} multiline textAlignVertical="top" />
          {!!error && <AppText style={styles.error} accessibilityRole="alert">{error}</AppText>}
          <TouchableOpacity accessibilityRole="button" style={styles.button} onPress={publish} disabled={saving}>
            {saving ? <ActivityIndicator color={theme.color.onAction} /> : <AppText style={styles.buttonText}>Publicar</AppText>}
          </TouchableOpacity>
        </ScrollView>
      </SafeAreaView>
    </Modal>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.color.background },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, backgroundColor: theme.color.surface, borderBottomWidth: 1, borderBottomColor: theme.color.border },
  title: { fontSize: 20, fontWeight: '700' },
  close: { color: theme.color.action, fontWeight: '600' },
  body: { padding: 20, gap: 8 },
  label: { fontSize: 13, fontWeight: '600', marginTop: 8 },
  input: { borderWidth: 1, borderColor: theme.color.borderStrong, borderRadius: 10, padding: 12, backgroundColor: theme.color.surface, fontFamily: theme.font.regular, fontSize: 15 },
  textarea: { minHeight: 140 },
  chips: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  chip: { borderWidth: 1, borderColor: theme.color.border, borderRadius: 999, paddingHorizontal: 12, paddingVertical: 6, backgroundColor: theme.color.surface },
  chipActive: { backgroundColor: theme.color.action, borderColor: theme.color.action },
  chipText: { fontSize: 13, color: theme.color.ink },
  chipTextActive: { color: theme.color.onAction, fontWeight: '600' },
  error: { color: theme.color.danger, marginTop: 8 },
  button: { backgroundColor: theme.color.action, borderRadius: 10, padding: 14, alignItems: 'center', marginTop: 16 },
  buttonText: { color: theme.color.onAction, fontWeight: '700', fontSize: 16 },
});
