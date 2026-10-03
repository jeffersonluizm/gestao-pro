import React, { createContext, useContext, useEffect, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { Platform } from 'react-native';
import { setAuthToken, setOnUnauthorized } from '../api/client';

function isJwtExpired(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    return typeof payload.exp === 'number' && payload.exp * 1000 < Date.now();
  } catch {
    return true;
  }
}

interface Session {
  token: string;
  userId: string;
  userName: string;
  role: string;
  condominiumId: string;
  registrationNumber: string;
}

interface AuthContextType {
  session: Session | null;
  loading: boolean;
  signIn: (session: Session) => Promise<void>;
  signOut: () => Promise<void>;
  updateName: (name: string) => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

const KEYS = {
  token: 'gestaopro_token',
  userId: 'gestaopro_user_id',
  userName: 'gestaopro_user_name',
  role: 'gestaopro_role',
  condominiumId: 'gestaopro_condominium_id',
  registrationNumber: 'gestaopro_registration_number',
};

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<Session | null>(null);
  const [loading, setLoading] = useState(true);

  // Register auto-logout: when any API call returns 401, clear session immediately.
  useEffect(() => {
    setOnUnauthorized(async () => {
      await AsyncStorage.multiRemove(Object.values(KEYS));
      setSession(null);
    });
  }, []);

  useEffect(() => {
    (async () => {
      // Na web (testes/demo) sempre inicia na tela de login
      if (Platform.OS === 'web') {
        await AsyncStorage.multiRemove(Object.values(KEYS));
        setLoading(false);
        return;
      }

      const token = await AsyncStorage.getItem(KEYS.token);
      if (token && !isJwtExpired(token)) {
        setAuthToken(token);
        setSession({
          token,
          userId: (await AsyncStorage.getItem(KEYS.userId)) ?? '',
          userName: (await AsyncStorage.getItem(KEYS.userName)) ?? '',
          role: (await AsyncStorage.getItem(KEYS.role)) ?? '',
          condominiumId: (await AsyncStorage.getItem(KEYS.condominiumId)) ?? '',
          registrationNumber: (await AsyncStorage.getItem(KEYS.registrationNumber)) ?? '',
        });
      } else if (token) {
        // Token expirado — limpa e redireciona para login
        await AsyncStorage.multiRemove(Object.values(KEYS));
      }
      setLoading(false);
    })();
  }, []);

  async function signIn(s: Session) {
    setAuthToken(s.token);
    await AsyncStorage.multiSet([
      [KEYS.token, s.token],
      [KEYS.userId, s.userId],
      [KEYS.userName, s.userName],
      [KEYS.role, s.role],
      [KEYS.condominiumId, s.condominiumId],
      [KEYS.registrationNumber, s.registrationNumber],
    ]);
    setSession(s);
  }

  async function signOut() {
    setAuthToken(null);
    await AsyncStorage.multiRemove(Object.values(KEYS));
    setSession(null);
  }

  async function updateName(name: string) {
    await AsyncStorage.setItem(KEYS.userName, name);
    setSession(prev => prev ? { ...prev, userName: name } : prev);
  }

  return (
    <AuthContext.Provider value={{ session, loading, signIn, signOut, updateName }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
