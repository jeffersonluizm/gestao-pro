# 0005. Trava por área e data contra reservas simultâneas

**Status:** aceita.

## Contexto
A reserva conferia conflito de horário e depois gravava. Dois pedidos ao mesmo tempo passavam juntos pela conferência (no teste, 20 de 20 pedidos simultâneos foram aceitos). Um índice único não resolve sobreposição de intervalos (18:00 a 20:00 contra 19:00 a 21:00).

## Decisão
Conferência e gravação rodam sob uma trava por **área comum + data** (`IReservationSlotLock`): em produção, um documento com `_id` único na coleção `reservation_locks`, com expiração por TTL para não travar para sempre se o processo cair; nos testes, uma trava em memória.

## Consequências
- Funciona com mais de uma instância da API e sem exigir transação ou replica set no MongoDB.
- Pedidos para a mesma área e data esperam numa fila curta; áreas ou datas diferentes não esperam.
- Verificado no teste de estresse (H6).
