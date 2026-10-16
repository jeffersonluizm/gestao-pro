import React, { useEffect, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import { ActivityIndicator, View } from 'react-native';
import { useAuth } from '../context/AuthContext';
import { sw, sf } from '../utils/responsive';
import { theme } from '../theme/theme';
import LoginScreen from '../screens/auth/LoginScreen';
import RegisterScreen from '../screens/auth/RegisterScreen';
import OnboardingScreen, { ONBOARDING_KEY } from '../screens/auth/OnboardingScreen';
import HomeScreen from '../screens/main/HomeScreen';
import ReservationsScreen from '../screens/main/ReservationsScreen';
import OccurrencesScreen from '../screens/main/OccurrencesScreen';
import ProfileScreen from '../screens/main/ProfileScreen';
import EncomendasScreen from '../screens/main/EncomendasScreen';
import PetsScreen from '../screens/main/PetsScreen';
import VotosScreen from '../screens/main/VotosScreen';
import AssembleiasScreen from '../screens/main/AssembleiasScreen';
import MaisScreen from '../screens/main/MaisScreen';
import CondominiosScreen from '../screens/main/CondominiosScreen';
import UsuariosScreen from '../screens/main/UsuariosScreen';
import UnidadesScreen from '../screens/main/UnidadesScreen';
import AreasComunsScreen from '../screens/main/AreasComunsScreen';
import VisitantesScreen from '../screens/main/VisitantesScreen';
import FinanceiroScreen from '../screens/main/FinanceiroScreen';

export type AuthStackParamList = {
  Onboarding: undefined;
  Login: undefined;
  Register: undefined;
};

export type TabParamList = {
  Início: undefined;
  Ocorrências: undefined;
  Reservas: undefined;
  Perfil: undefined;
  Mais: undefined;
};

// Telas abertas pelo "Mais": numa pilha própria, com voltar (botão e gesto/tecla do sistema).
export type MaisStackParamList = {
  MaisInicio: undefined;
  Assembleias: undefined;
  Votos: undefined;
  Encomendas: undefined;
  Financeiro: undefined;
  Pets: undefined;
  Condomínios: undefined;
  Usuários: undefined;
  Unidades: undefined;
  'Áreas Comuns': undefined;
  Visitantes: undefined;
};

const AuthStack = createNativeStackNavigator<AuthStackParamList>();
const Tab = createBottomTabNavigator<TabParamList>();
const Mais = createNativeStackNavigator<MaisStackParamList>();

// Ícones das 5 abas visíveis
const TAB_ICONS: Record<keyof TabParamList, { active: string; inactive: string }> = {
  'Início': { active: 'home', inactive: 'home-outline' },
  'Ocorrências': { active: 'construct', inactive: 'construct-outline' },
  'Reservas': { active: 'calendar', inactive: 'calendar-outline' },
  'Perfil': { active: 'person-circle', inactive: 'person-circle-outline' },
  'Mais': { active: 'grid', inactive: 'grid-outline' },
};

function MaisStack() {
  return (
    <Mais.Navigator screenOptions={{ headerShown: false }}>
      <Mais.Screen name="MaisInicio" component={MaisScreen} />
      <Mais.Screen name="Assembleias" component={AssembleiasScreen} />
      <Mais.Screen name="Votos" component={VotosScreen} />
      <Mais.Screen name="Encomendas" component={EncomendasScreen} />
      <Mais.Screen name="Financeiro" component={FinanceiroScreen} />
      <Mais.Screen name="Pets" component={PetsScreen} />
      <Mais.Screen name="Condomínios" component={CondominiosScreen} />
      <Mais.Screen name="Usuários" component={UsuariosScreen} />
      <Mais.Screen name="Unidades" component={UnidadesScreen} />
      <Mais.Screen name="Áreas Comuns" component={AreasComunsScreen} />
      <Mais.Screen name="Visitantes" component={VisitantesScreen} />
    </Mais.Navigator>
  );
}

function MainTabs() {
  return (
    <Tab.Navigator
      backBehavior="history"
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarActiveTintColor: theme.color.action,
        tabBarInactiveTintColor: theme.color.inkMuted,
        tabBarStyle: {
          borderTopColor: theme.color.border,
          paddingBottom: sw(6),
          paddingTop: sw(4),
          height: sw(62),
        },
        tabBarLabelStyle: { fontSize: sf(11), fontFamily: theme.font.semibold },
        tabBarIcon: ({ color, focused }) => {
          const pair = TAB_ICONS[route.name];
          return <Ionicons name={(focused ? pair.active : pair.inactive) as never} size={sw(24)} color={color} />;
        },
      })}
    >
      <Tab.Screen name="Início" component={HomeScreen} />
      <Tab.Screen name="Ocorrências" component={OccurrencesScreen} />
      <Tab.Screen name="Reservas" component={ReservationsScreen} />
      <Tab.Screen name="Perfil" component={ProfileScreen} />
      <Tab.Screen name="Mais" component={MaisStack} />
    </Tab.Navigator>
  );
}

export default function AppNavigator() {
  const { session, loading } = useAuth();
  const [onboardingSeen, setOnboardingSeen] = useState<boolean | null>(null);

  useEffect(() => {
    AsyncStorage.getItem(ONBOARDING_KEY)
      .then(value => setOnboardingSeen(value === '1'))
      .catch(() => setOnboardingSeen(true));
  }, []);

  if (loading || onboardingSeen === null) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: theme.color.brand }}>
        <ActivityIndicator size="large" color={theme.color.brandAccent} accessibilityLabel="Carregando" />
      </View>
    );
  }

  return (
    <NavigationContainer>
      {session ? (
        <MainTabs />
      ) : (
        <AuthStack.Navigator screenOptions={{ headerShown: false }} initialRouteName={onboardingSeen ? 'Login' : 'Onboarding'}>
          <AuthStack.Screen name="Onboarding" component={OnboardingScreen} />
          <AuthStack.Screen name="Login" component={LoginScreen} />
          <AuthStack.Screen name="Register" component={RegisterScreen} />
        </AuthStack.Navigator>
      )}
    </NavigationContainer>
  );
}
