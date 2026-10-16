import React, { useState } from 'react';
import { Platform, StyleSheet, TextInput, TouchableOpacity, View } from 'react-native';
import DateTimePicker, { DateTimePickerEvent } from '@react-native-community/datetimepicker';
import { Ionicons } from '@expo/vector-icons';
import AppText from './AppText';
import { theme } from '../theme/theme';
import { maskDateBR, maskTime } from '../utils/formatters';
import { dateFromDisplay, displayDate, displayTime, timeFromDisplay } from '../utils/pickerValues';

// Campo de data ou hora: seletor nativo no Android/iOS; no navegador (Expo web), texto com máscara.
export default function DateTimeField({
  label, mode, value, onChange, minimumDate,
}: {
  label: string;
  mode: 'date' | 'time';
  value: string;
  onChange: (text: string) => void;
  minimumDate?: Date;
}) {
  const [open, setOpen] = useState(false);
  const placeholder = mode === 'date' ? 'DD/MM/AAAA' : 'HH:MM';

  if (Platform.OS === 'web') {
    return (
      <View>
        <AppText style={styles.label}>{label}</AppText>
        <TextInput
          style={styles.input}
          accessibilityLabel={label}
          placeholder={placeholder}
          keyboardType="numeric"
          maxLength={mode === 'date' ? 10 : 5}
          value={value}
          onChangeText={text => onChange(mode === 'date' ? maskDateBR(text) : maskTime(text))}
        />
      </View>
    );
  }

  const current = (mode === 'date' ? dateFromDisplay(value) : timeFromDisplay(value)) ?? new Date();

  function handle(event: DateTimePickerEvent, selected?: Date) {
    if (Platform.OS === 'android') setOpen(false);
    if (event.type === 'set' && selected) onChange(mode === 'date' ? displayDate(selected) : displayTime(selected));
  }

  return (
    <View>
      <AppText style={styles.label}>{label}</AppText>
      <TouchableOpacity
        accessibilityRole="button"
        accessibilityLabel={`${label}: ${value || 'não escolhido'}`}
        accessibilityHint={mode === 'date' ? 'Abre o calendário' : 'Abre o seletor de horário'}
        style={[styles.input, styles.row]}
        onPress={() => setOpen(true)}
      >
        <AppText style={{ color: value ? theme.color.ink : theme.color.inkMuted }}>{value || placeholder}</AppText>
        <Ionicons name={mode === 'date' ? 'calendar-outline' : 'time-outline'} size={20} color={theme.color.action} />
      </TouchableOpacity>
      {open && (
        <DateTimePicker
          value={current}
          mode={mode}
          is24Hour
          display={Platform.OS === 'ios' ? 'inline' : 'default'}
          minimumDate={minimumDate}
          onChange={handle}
          locale="pt-BR"
        />
      )}
      {open && Platform.OS === 'ios' && (
        <TouchableOpacity accessibilityRole="button" style={styles.done} onPress={() => setOpen(false)}>
          <AppText style={{ color: theme.color.action, fontWeight: '700' }}>Concluir</AppText>
        </TouchableOpacity>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  label: { fontSize: 13, fontWeight: '600', marginTop: 12, marginBottom: 4 },
  input: { borderWidth: 1, borderColor: theme.color.borderStrong, borderRadius: 10, padding: 12, backgroundColor: theme.color.surface, fontFamily: theme.font.regular, fontSize: 15 },
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  done: { alignSelf: 'flex-end', padding: 8 },
});
