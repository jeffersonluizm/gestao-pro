import { API_BASE_URL } from '../config';
import AsyncStorage from '@react-native-async-storage/async-storage';

// Module-level cache so we don't read AsyncStorage on every request.
// AuthContext calls setAuthToken() on login/logout/restore for the fast path.
// The request() function falls back to AsyncStorage so HMR / edge-cases work.
let _authToken: string | null = null;
export function setAuthToken(token: string | null) { _authToken = token; }

// Called by AuthContext so client can trigger logout when token is rejected (401).
let _onUnauthorized: (() => void) | null = null;
export function setOnUnauthorized(cb: () => void) { _onUnauthorized = cb; }

// ─── Tipos ────────────────────────────────────────────────────────────────────
export interface LoginResponse {
  token: string;
  userId: string;
  userName: string;
  registrationNumber: string;
  role: string;
  condominiumId: string;
  mustChangePassword: boolean;
  expiresAt: string;
}

// A API só diz se o cadastro pode seguir: sem nome nem motivo da recusa.
export interface CheckCpfResponse {
  canProceed: boolean;
  needsProfileData: boolean;
  message: string;
}

export interface SelfRegisterResponse {
  registrationNumber: string;
  name: string;
}

export interface Condominium {
  id: string;
  name: string;
  cnpj: string;
  street: string;
  number: string;
  neighborhood: string;
  city: string;
  state: string;
  zipCode: string;
  rules?: string;
  regulations?: string;
  residentCount: number;
}

export interface Announcement {
  id: string;
  title: string;
  content: string;
  category: string;
  createdAt: string;
  expiresAt?: string;
}

export interface CommonArea {
  id: string;
  condominiumId: string;
  name: string;
  description: string;
  capacity: number;
  openTime: string;
  closeTime: string;
  rules: string;
  isActive: boolean;
}

export interface Reservation {
  id: string;
  commonAreaId: string;
  userId: string;
  date: string;
  startTime: string;
  endTime: string;
  status: string;
  observations?: string;
}

export interface Occurrence {
  id: string;
  title: string;
  description: string;
  category: string;
  priority: string;
  status: string;
  createdAt: string;
}

export interface Delivery {
  id: string;
  trackingCode: string;
  type: string;
  description: string;
  residentId: string;
  unitId: string;
  recipientName?: string;
  unitNumber?: string;
  status: string;
  arrivedAt: string;
  pickedUpAt?: string;
  pickedUpBy?: string;
  observations?: string;
  qrCodeBase64?: string;
  fotoUrl?: string;
}

export interface User {
  id: string;
  _id?: string | { $oid?: string };
  userId?: string;
  usuarioId?: string;
  condominiumId: string;
  unitId?: string;
  name: string;
  email: string;
  phone: string;
  cpf: string;
  role: string;
  registrationNumber: string;
  birthDate?: string;
  isActive: boolean;
}

export interface Unit {
  id: string;
  condominiumId: string;
  number: string;
  block?: string;
  floor?: number;
  type: string;
  residentIds: string[];
  residentCount: number;
}

export interface Visitor {
  id?: string;
  _id?: string | { $oid?: string };
  visitorId?: string;
  fullName: string;
  document: string;
  phone: string;
  residentId: string;
  condominiumId?: string;
  unitId: string;
  reason: string;
  status: string;
  checkInAt?: string;
  checkOutAt?: string;
  createdAt: string;
}

export type PetSpecies = 'Dog' | 'Cat' | 'Bird' | 'Rabbit' | 'Other';

export interface Pet {
  id: string;
  _id?: string | { $oid?: string };
  ownerId: string;
  unitId: string;
  condominiumId: string;
  name: string;
  species: PetSpecies;
  breed: string;
  color: string;
  vaccination: boolean;
  observations: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

function friendlyApiError(text: string, status: number) {
  if (status >= 500) return 'Erro interno no servidor. Tente novamente mais tarde.';

  const validationMessage = validationErrorMessage(text);
  if (validationMessage) return validationMessage;

  try {
    const data = JSON.parse(text) as { message?: unknown; title?: unknown };
    if (typeof data.message === 'string' && isSafeMessage(data.message)) return data.message.trim();
    if (
      typeof data.title === 'string'
      && isSafeMessage(data.title)
      && !/bad request|internal server error|validation errors occurred/i.test(data.title)
    ) return data.title.trim();
  } catch {
    const plainText = text.trim();
    if (/morador n[aã]o pertence a essa unidade/i.test(plainText)) {
      return 'O morador selecionado não pertence à unidade escolhida.';
    }
    if (isSafeMessage(plainText)) return plainText;
  }

  if (status === 400) return 'Verifique os campos informados.';
  if (status === 401) return 'Sua sessão expirou. Faça login novamente.';
  if (status === 403) return 'Você não tem permissão para realizar esta ação.';
  if (status === 404) return 'Registro não encontrado.';
  if (status === 409) return 'Já existe um registro com essas informações.';
  return 'Não foi possível concluir a operação. Tente novamente.';
}

const requiredFieldMessages: Record<string, string> = {
  name: 'Informe o nome.',
  fullname: 'Informe o nome completo.',
  email: 'Informe o e-mail.',
  cpf: 'Informe o CPF.',
  cnpj: 'Informe o CNPJ.',
  zipcode: 'Informe o CEP.',
  birthdate: 'Informe a data de nascimento.',
  title: 'Informe o título.',
  description: 'Informe a descrição.',
  condominiumid: 'Selecione o condomínio.',
  unitid: 'Selecione a unidade.',
  residentid: 'Selecione o morador.',
  ownerid: 'Selecione o tutor.',
};

function normalizeValidationKey(key: string) {
  return key.replace(/^\$\./, '').split('.').pop()?.replace(/[^a-z0-9]/gi, '').toLowerCase() ?? '';
}

function friendlyFieldError(key: string, message: string) {
  const field = normalizeValidationKey(key);
  const normalizedMessage = message.trim();

  if (field.includes('cpf') || /\bcpf\b/i.test(normalizedMessage)) {
    return 'Informe um CPF válido no formato 000.000.000-00.';
  }
  if (field.includes('cnpj') || /\bcnpj\b/i.test(normalizedMessage)) {
    return 'Informe um CNPJ válido no formato 00.000.000/0000-00.';
  }
  if (field.includes('zipcode') || /\bcep\b|zip.?code/i.test(normalizedMessage)) {
    return 'Informe um CEP válido no formato 00000-000.';
  }
  if (field.includes('date') || /date.*convert|data inválida|data invalida/i.test(normalizedMessage)) {
    return 'Data inválida. Use DD/MM/AAAA.';
  }
  if (/email/i.test(field) && /invalid|inválid/i.test(normalizedMessage)) {
    return 'Informe um e-mail válido.';
  }
  if (/required|obrigat/i.test(normalizedMessage)) {
    return requiredFieldMessages[field] ?? 'Preencha os campos obrigatórios.';
  }
  if (isSafeMessage(normalizedMessage)) return normalizedMessage;
  return requiredFieldMessages[field] ?? '';
}

function validationErrorMessage(text: string) {
  try {
    const data = JSON.parse(text) as { errors?: Record<string, unknown> };
    if (!data.errors || typeof data.errors !== 'object') return '';
    const messages = Object.entries(data.errors)
      .flatMap(([key, value]) => {
        const values = Array.isArray(value) ? value : [value];
        return values
          .filter((item): item is string => typeof item === 'string')
          .map(item => friendlyFieldError(key, item));
      })
      .filter(Boolean);
    return [...new Set(messages)].join('\n');
  } catch {}
  return '';
}

function isSafeMessage(message: string) {
  const normalized = message.trim();
  return Boolean(normalized)
    && normalized.length <= 500
    && !/[{}<>]/.test(normalized)
    && !/\bat\s+\S+\.\S+|exception|stack trace|developerexceptionpage|could not be converted|json value|unexpected token|syntaxerror|object object|validation errors occurred/i.test(normalized);
}

export function getFriendlyErrorMessage(error: unknown, fallback = 'Não foi possível concluir a operação. Tente novamente.') {
  if (error instanceof TypeError && /network|fetch|conectar|failed/i.test(error.message)) {
    return 'Não foi possível conectar ao servidor.';
  }
  if (error instanceof Error && isSafeMessage(error.message)) return error.message;
  if (error && typeof error === 'object') {
    const record = error as Record<string, unknown>;
    const response = record.response && typeof record.response === 'object'
      ? record.response as Record<string, unknown>
      : undefined;
    const status = Number(record.status ?? response?.status ?? 0);
    const data = response?.data ?? record;
    try {
      const message = friendlyApiError(JSON.stringify(data), status);
      if (message !== 'Não foi possível concluir a operação. Tente novamente.') return message;
    } catch {}
  }
  return fallback;
}

// ─── Helper HTTP ──────────────────────────────────────────────────────────────
async function request<T>(
  path: string,
  options: RequestInit = {},
  requiresAuth = true,
): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  };

  if (requiresAuth) {
    // Fast path: use the cached module-level token set by AuthContext.
    // Fallback: read from AsyncStorage (handles HMR resets, page refreshes, etc.)
    const token = _authToken ?? await AsyncStorage.getItem('gestaopro_token');
    if (token) {
      if (!_authToken) _authToken = token; // heal the cache
      headers['Authorization'] = `Bearer ${token}`;
    }
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers });
  } catch (error) {
    console.error('Erro de rede na API:', error);
    throw new Error('Não foi possível conectar ao servidor.');
  }

  if (!response.ok) {
    if (response.status === 401) {
      _authToken = null;
      _onUnauthorized?.();
      throw new Error('Sua sessão expirou. Faça login novamente.');
    }
    const text = await response.text();
    throw new Error(friendlyApiError(text, response.status));
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

// ─── Auth ─────────────────────────────────────────────────────────────────────
export const login = (email: string, password: string) =>
  request<LoginResponse>('/api/Auth/login', {
    method: 'POST',
    body: JSON.stringify({ registrationNumber: email, password }),
  }, false);

export const checkCpf = (cpf: string, condominiumId: string, condominiumCode: string) =>
  request<CheckCpfResponse>('/api/auth/check-cpf', {
    method: 'POST',
    body: JSON.stringify({ cpf, condominiumId, condominiumCode }),
  }, false);

export const selfRegister = (data: {
  cpf: string;
  condominiumId: string;
  condominiumCode: string;
  password: string;
  name?: string;
  email?: string;
  phone?: string;
}) =>
  request<SelfRegisterResponse>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  }, false);

export const changePasswordOnFirstLogin = (userId: string, currentPassword: string, newPassword: string) =>
  request<void>('/api/Auth/change-password', {
    method: 'PATCH',
    body: JSON.stringify({ userId, currentPassword, newPassword }),
  }, false);

export const changeUserPassword = (userId: string, currentPassword: string, newPassword: string) =>
  request<void>(`/api/Users/${encodeURIComponent(userId)}/change-password`, {
    method: 'PATCH',
    body: JSON.stringify({ currentPassword, newPassword }),
  });

// ─── Condomínios ──────────────────────────────────────────────────────────────
export const getCondominiums = () =>
  request<Condominium[]>('/api/Condominiums');

export const createCondominium = (data: {
  name: string;
  cnpj: string;
  street: string;
  number: string;
  neighborhood: string;
  city: string;
  state: string;
  zipCode: string;
  rules?: string;
  regulations?: string;
  // Código do condomínio (senha padrão): obrigatório no cadastro, forte (a API recusa código óbvio).
  defaultPassword: string;
}) =>
  request<Condominium>('/api/Condominiums', {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const updateCondominium = (id: string, data: {
  name: string;
  cnpj: string;
  street: string;
  number: string;
  neighborhood: string;
  city: string;
  state: string;
  zipCode: string;
  rules?: string;
  regulations?: string;
  // Em branco mantém o código atual.
  defaultPassword?: string;
}) =>
  request<Condominium>(`/api/Condominiums/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });

export const deleteCondominium = (id: string) =>
  request<void>(`/api/Condominiums/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });

// ─── Usuário ──────────────────────────────────────────────────────────────────
export const getUserById = (id: string) =>
  request<User>(`/api/Users/${encodeURIComponent(id)}`);

export const getUsers = () =>
  request<User[]>('/api/Users');

export const getUsersByCondominium = (condominiumId: string) =>
  request<User[]>(`/api/Users/condominium/${encodeURIComponent(condominiumId)}`);

export const createUser = (data: {
  condominiumId: string;
  unitId?: string;
  name: string;
  email: string;
  password: string;
  phone: string;
  cpf: string;
  role: string;
  birthDate?: string;
}) =>
  request<User>('/api/Users', {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const updateUser = (id: string, data: {
  condominiumId: string;
  unitId?: string;
  name: string;
  email: string;
  password: string;
  phone: string;
  cpf: string;
  role: string;
  birthDate?: string;
}) =>
  request<User>(`/api/Users/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });

export const deactivateUser = (id: string) => {
  const url = `/api/Users/${encodeURIComponent(id)}`;
  return request<void>(url, {
    method: 'DELETE',
  });
};

export const getUnitsByCondominium = (condominiumId: string) =>
  request<Unit[]>(`/api/Units/condominium/${encodeURIComponent(condominiumId)}`);

export const createUnit = (data: {
  condominiumId: string;
  number: string;
  block?: string;
  floor?: number;
  type: string;
}) =>
  request<Unit>('/api/Units', {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const updateUnit = (id: string, data: {
  condominiumId: string;
  number: string;
  block?: string;
  floor?: number;
  type: string;
}) =>
  request<Unit>(`/api/Units/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });

export const deleteUnit = (id: string) =>
  request<void>(`/api/Units/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });

// ─── Avisos ───────────────────────────────────────────────────────────────────
export const getAnnouncements = () =>
  request<Announcement[]>('/api/announcements');

// ─── Áreas Comuns ─────────────────────────────────────────────────────────────
export const getCommonAreas = () =>
  request<CommonArea[]>('/api/CommonAreas');

export const createCommonArea = (data: {
  condominiumId: string;
  name: string;
  description?: string;
  capacity: number;
  openTime: string;
  closeTime: string;
  rules?: string;
}) =>
  request<CommonArea>('/api/CommonAreas', {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const updateCommonArea = (id: string, data: {
  condominiumId: string;
  name: string;
  description?: string;
  capacity: number;
  openTime: string;
  closeTime: string;
  rules?: string;
}) =>
  request<CommonArea>(`/api/CommonAreas/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });

export const deleteCommonArea = (id: string) =>
  request<void>(`/api/CommonAreas/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });

// ─── Visitantes ────────────────────────────────────────────────────────────────
export const getVisitors = () =>
  request<Visitor[]>('/api/Visitor');

export const getVisitorsByResident = (residentId: string) =>
  request<Visitor[]>(`/api/Visitor/resident/${encodeURIComponent(residentId)}`);

export const createVisitor = (data: {
  condominiumId?: string;
  fullName: string;
  document: string;
  phone?: string;
  residentId: string;
  unitId: string;
  reason: string;
  status?: string;
}) =>
  request<Visitor>('/api/Visitor/register', {
    method: 'POST',
    headers: { 'X-User-Id': data.residentId },
    body: JSON.stringify(data),
  });

export const updateVisitor = (id: string, data: {
  condominiumId?: string;
  fullName: string;
  document: string;
  phone?: string;
  residentId: string;
  unitId: string;
  reason: string;
  status?: string;
}) =>
  request<Visitor>(`/api/Visitor/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'X-User-Id': data.residentId },
    body: JSON.stringify(data),
  });

export const deleteVisitor = (id: string) => {
  const endpoint = `/api/Visitor/${encodeURIComponent(id)}`;
  return request<void>(endpoint, { method: 'DELETE' });
};

export const checkInVisitor = (id: string) =>
  request<void>(`/api/Visitor/check-in/${encodeURIComponent(id)}`, { method: 'POST' });

export const checkOutVisitor = (id: string) =>
  request<void>(`/api/Visitor/check-out/${encodeURIComponent(id)}`, { method: 'POST' });

// ─── Reservas ─────────────────────────────────────────────────────────────────
export const getMyReservations = (userId: string) =>
  request<Reservation[]>(`/api/reservations/user/${encodeURIComponent(userId)}`);

export const createReservation = (data: {
  commonAreaId: string;
  userId: string;
  condominiumId: string;
  date: string;
  startTime: string;
  endTime: string;
  observations?: string;
}) =>
  request<Reservation>('/api/reservations', {
    method: 'POST',
    body: JSON.stringify({ ...data, status: 'pendente' }),
  });

export const deleteReservation = (id: string) =>
  request<void>(`/api/reservations/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });

// ─── Ocorrências ──────────────────────────────────────────────────────────────
export const getMyOccurrences = (userId: string) =>
  request<Occurrence[]>(`/api/Occurrences/user/${encodeURIComponent(userId)}`);

export const createOccurrence = (data: {
  title: string;
  description: string;
  category: string;
  priority: string;
  userId: string;
  condominiumId: string;
}) =>
  request<Occurrence>('/api/Occurrences', {
    method: 'POST',
    body: JSON.stringify(data),
    // OccurrencesController reads userId and condominiumId from custom headers, not the body
    headers: {
      'X-User-Id': data.userId,
      'X-Condominium-Id': data.condominiumId,
    },
  });

export const deleteOccurrence = (id: string) =>
  request<void>(`/api/Occurrences/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });

// ─── Entregas ─────────────────────────────────────────────────────────────────
export const getDeliveries = () =>
  request<Delivery[]>('/api/Delivery');

export const getDeliveriesByResident = (residentId: string) =>
  request<Delivery[]>(`/api/Delivery/resident/${encodeURIComponent(residentId)}`);

export const createDelivery = (data: {
  trackingCode: string;
  description?: string;
  type?: string;
  residentId: string;
  unitId?: string;
  status?: string;
  observations?: string;
  fotoUrl?: string;
}) =>
  request<Delivery>('/api/Delivery', {
    method: 'POST',
    body: JSON.stringify({
      type: 'encomenda',
      status: 'pending',
      description: '',
      unitId: '',
      ...data,
    }),
  });

export const updateDelivery = (id: string, data: {
  trackingCode?: string;
  description?: string;
  type?: string;
  residentId?: string;
  unitId?: string;
  status?: string;
  pickedUpBy?: string;
  observations?: string;
  qrCodeBase64?: string;
  fotoUrl?: string;
}) =>
  request<Delivery>(`/api/Delivery/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });

export const pickupDelivery = (id: string, pickedUpBy: string) =>
  request<void>(`/api/Delivery/${encodeURIComponent(id)}/pickup?pickedUpBy=${encodeURIComponent(pickedUpBy)}`, {
    method: 'PUT',
  });

export const deleteDelivery = (id: string) =>
  request<void>(`/api/Delivery/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });

export const getPetsByCondominium = (condominiumId: string) =>
  request<Pet[]>(`/api/Pets/condominium/${encodeURIComponent(condominiumId)}`);

export const getPetsByUnit = (unitId: string) =>
  request<Pet[]>(`/api/Pets/unit/${encodeURIComponent(unitId)}`);

export const getPetsByOwner = (ownerId: string) =>
  request<Pet[]>(`/api/Pets/owner/${encodeURIComponent(ownerId)}`);

export const createPet = (data: {
  ownerId: string;
  unitId: string;
  condominiumId: string;
  name: string;
  species: PetSpecies;
  breed?: string;
  color?: string;
  vaccination: boolean;
  observations?: string;
}) =>
  request<Pet>('/api/Pets', {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const updatePet = (id: string, data: {
  name?: string;
  species?: PetSpecies;
  breed?: string;
  color?: string;
  vaccination?: boolean;
  observations?: string;
  isActive?: boolean;
}) =>
  request<Pet>(`/api/Pets/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });

export const deletePet = (id: string) =>
  request<void>(`/api/Pets/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });

// ─── Assembleias / Votos ────────────────────────────────────────────────────
export interface Assembly {
  id: string;
  condominiumId: string;
  createdBy?: string;
  title: string;
  description: string;
  startTime: string;
  endTime: string;
  status: string;
  topics: { title: string; desc?: string; voteOptions?: string[] }[];
  minutes?: string;
  createdAt?: string;
  updatedAt?: string;
  voteStarted?: boolean;
  voteFinished?: boolean;
  voteStatus?: string;
  startDate?: string;
  endDate?: string;
}

export interface VoteResponse {
  id: string;
  assemblyId: string;
  assemblyTitle?: string;
  assemblyDescription?: string;
  topicDescription?: string;
  topicIndex: number;
  voto: string; // resposta do backend (pode ser 'Privado' para não administradores)
  userId: string;
  yesVotes?: number;
  noVotes?: number;
  abstentionVotes?: number;
  totalVotes?: number;
  optionCounts?: Record<string, number>;
  summaryOnly?: boolean;
  winner?: string;
  voteStarted?: boolean;
  voteFinished?: boolean;
  voteStatus?: string;
  voteStartDate?: string;
  voteEndDate?: string;
  status?: string;
  createdAt?: string;
  [key: string]: any;
}

export const getAssemblies = (history = false) =>
  request<Assembly[]>(`/api/Assembly${history ? '?history=true' : ''}`);

export const getAssemblyById = (id: string) =>
  request<Assembly>(`/api/Assembly/${encodeURIComponent(id)}`);

export const getAssembliesByCondominium = (condominiumId: string, history = false) =>
  request<Assembly[]>(`/api/Assembly/condominium/${encodeURIComponent(condominiumId)}${history ? '?history=true' : ''}`);

export const createAssembly = (data: {
  condominiumId: string;
  createdBy?: string;
  title: string;
  description?: string;
  scheduledDate: string;
  startDate?: string;
  endDate?: string;
  topicsText?: string;
  topics?: { title: string; desc?: string; voteOptions?: string[] }[];
}) =>
  request<Assembly>('/api/Assembly', {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const updateAssembly = (id: string, data: {
  title?: string;
  description?: string;
  scheduledDate?: string;
  startDate?: string;
  endDate?: string;
  status?: string;
  minutes?: string;
  topicsText?: string;
  topics?: { title: string; desc?: string; voteOptions?: string[] }[];
}) =>
  request<Assembly>(`/api/Assembly/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });

export const deleteAssembly = (id: string) =>
  request<void>(`/api/Assembly/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });

export const getVotesByAssembly = (assemblyId: string) =>
  request<VoteResponse[]>(`/api/Vote/assembly/${encodeURIComponent(assemblyId)}`);

export const castVote = (data: {
  assemblyId: string;
  vote: string;
  userId?: string;
  topicIndex?: number;
}) =>
  request<VoteResponse>('/api/Vote', {
    method: 'POST',
    body: JSON.stringify(data),
  });
