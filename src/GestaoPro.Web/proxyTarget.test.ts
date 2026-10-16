import { describe, expect, it } from 'vitest'
import { resolveProxyTarget } from './proxyTarget'

describe('resolveProxyTarget', () => {
  it('aponta para a API local na porta 5088 quando nada foi configurado', () => {
    expect(resolveProxyTarget({})).toBe('http://localhost:5088')
  })

  it('usa VITE_API_PROXY_TARGET quando definida', () => {
    expect(resolveProxyTarget({ VITE_API_PROXY_TARGET: 'https://api.exemplo.test' }))
      .toBe('https://api.exemplo.test')
  })

  it('ignora VITE_API_PROXY_TARGET vazia ou só com espaços', () => {
    expect(resolveProxyTarget({ VITE_API_PROXY_TARGET: '   ' })).toBe('http://localhost:5088')
  })
})
