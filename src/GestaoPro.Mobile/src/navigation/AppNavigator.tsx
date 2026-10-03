import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import { ActivityIndicator, View } from 'react-native';
import { useAuth } from '../context/AuthContext';
import { sw, sf, SCREEN } from '../utils/responsive';
import LoginScreen from '../screens/auth/LoginScreen';
import RegisterScreen from '../screens/auth/RegisterScreen';
import HomeScreen from '../screens/main/HomeScreen';
import ReservationsScreen from '../screens/main/ReservationsScreen';
import OccurrencesScreen from '../screens/main/OccurrencesScreen';
import DeliveriesScreen from '../screens/main/DeliveriesScreen';
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

export type AuthStackParamList = {
  Login: undefined;
  Register: undefined;
};

export type TabParamList = {
  Início: undefined;
  Ocorrências: undefined;
  Reservas: undefined;
  Perfil: undefined;
  Mais: undefined;
  // Telas acessadas via MaisScreen (ocultas na tab bar)
  Assembleias: undefined;
  Votos: undefined;
  Entregas: undefined;
  Encomendas: undefined;
  Pets: undefined;
  Condomínios: undefined;
  Usuários: undefined;
  Unidades: undefined;
  'Áreas Comuns': undefined;
  Visitantes: undefined;
};

const AuthStack = createNativeStackNavigator<AuthStackParamList>();
const Tab = createBottomTabNavigator<TabParamList>();

const PRIMARY = '#176b87';

// Ícones das 5 abas visíveis
const TAB_ICONS: Record<string, { active: string; inactive: string }> = {
  'Início':      { active: 'home',          inactive: 'home-outline' },
  'Ocorrências': { active: 'construct',     inactive: 'construct-outline' },
  'Reservas':    { active: 'calendar',      inactive: 'calendar-outline' },
  'Perfil':      { active: 'person-circle', inactive: 'person-circle-outline' },
  'Mais':        { active: 'grid',          inactive: 'grid-outline' },
};

function MainTabs() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarActiveTintColor: PRIMARY,
        tabBarInactiveTintColor: '#94a3b8',
        tabBarStyle: {
          borderTopColor: '#e2e8f0',
          paddingBottom: sw(6),
          paddingTop: sw(4),
          height: sw(60),
        },
        tabBarLabelStyle: { fontSize: sf(11), fontWeight: '600' },
        tabBarIcon: ({ color, focused }) => {
          const pair = TAB_ICONS[route.name];
          if (!pair) return null;
          const iconName = focused ? pair.active : pair.inactive;
          return <Ionicons name={iconName as any} size={sw(24)} color={color} />;
        },
      })}
    >
      {/* ── Abas visíveis ── */}
      <Tab.Screen name="Início"      component={HomeScreen} />
      <Tab.Screen name="Ocorrências" component={OccurrencesScreen} />
      <Tab.Screen name="Reservas"    component={ReservationsScreen} />
      <Tab.Screen name="Perfil"      component={ProfileScreen} />
      <Tab.Screen name="Mais"        component={MaisScreen} />

      {/* ── Telas ocultas na tab bar, acessíveis via MaisScreen ── */}
      <Tab.Screen name="Assembleias" component={AssembleiasScreen} options={{ tabBarButton: () => null }} />
      <Tab.Screen name="Votos"       component={VotosScreen}       options={{ tabBarButton: () => null }} />
      <Tab.Screen name="Entregas"    component={DeliveriesScreen}  options={{ tabBarButton: () => null }} />
      <Tab.Screen name="Encomendas"  component={EncomendasScreen}  options={{ tabBarButton: () => null }} />
      <Tab.Screen name="Pets"        component={PetsScreen}        options={{ tabBarButton: () => null }} />
      <Tab.Screen name="Condomínios" component={CondominiosScreen} options={{ tabBarButton: () => null }} />
      <Tab.Screen name="Usuários"    component={UsuariosScreen}    options={{ tabBarButton: () => null }} />
      <Tab.Screen name="Unidades"    component={UnidadesScreen}    options={{ tabBarButton: () => null }} />
      <Tab.Screen name="Áreas Comuns" component={AreasComunsScreen} options={{ tabBarButton: () => null }} />
      <Tab.Screen name="Visitantes"   component={VisitantesScreen}   options={{ tabBarButton: () => null }} />
    </Tab.Navigator>
  );
}

export default function AppNavigator() {
  const { session, loading } = useAuth();

  if (loading) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
        <ActivityIndicator size="large" color={PRIMARY} />
      </View>
    );
  }

  return (
    <NavigationContainer>
      {session ? (
        <MainTabs />
      ) : (
        <AuthStack.Navigator screenOptions={{ headerShown: false }}>
          <AuthStack.Screen name="Login"    component={LoginScreen} />
          <AuthStack.Screen name="Register" component={RegisterScreen} />
        </AuthStack.Navigator>
      )}
    </NavigationContainer>
  );
}
