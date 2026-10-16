"""Conferência de sintaxe do Dockerfile sem Docker: instruções conhecidas, continuação de linha real e ENV/ARG/LABEL
bem formados, sem "\\n" literal fora de aspas (o erro que um replace automático deixou no ENV).
Uso: python ferramentas/conferir_dockerfile.py Dockerfile"""
import re
import sys

INSTRUCOES = {'FROM', 'RUN', 'CMD', 'LABEL', 'EXPOSE', 'ENV', 'ADD', 'COPY', 'ENTRYPOINT', 'VOLUME', 'USER', 'WORKDIR',
              'ARG', 'ONBUILD', 'STOPSIGNAL', 'HEALTHCHECK', 'SHELL', 'MAINTAINER'}
TOKEN = re.compile(r'(?:[^\s"\']+|"[^"]*"|\'[^\']*\')+')


def fora_de_aspas(texto):
    return re.sub(r'"[^"]*"|\'[^\']*\'', '', texto)


def conferir_pares(instrucao, inicio, resto):
    erros = []
    if '\\n' in fora_de_aspas(resto):
        erros.append(f'linha {inicio}: "\\n" literal em {instrucao} (use uma linha por instrução ou "\\" no fim da linha)')
    tokens = TOKEN.findall(resto)
    if not tokens:
        return erros + [f'linha {inicio}: {instrucao} vazio']
    if instrucao == 'ARG':
        return erros
    if '=' not in tokens[0]:
        # Forma antiga: ENV CHAVE valor (uma chave só, o resto é o valor).
        if instrucao == 'ENV' and len(tokens) < 2:
            erros.append(f'linha {inicio}: ENV {tokens[0]} sem valor')
        return erros
    for token in tokens:
        if '=' not in token:
            erros.append(f'linha {inicio}: "{token}" sem chave=valor em {instrucao}')
    return erros


def problemas(texto):
    erros = []
    linhas = texto.replace('\r\n', '\n').split('\n')
    logica, inicio = '', 0
    for numero, linha in enumerate(linhas, 1):
        if not logica and (not linha.strip() or linha.lstrip().startswith('#')):
            continue
        if not logica:
            inicio = numero
        if linha.endswith('\\'):
            logica += linha[:-1] + ' '
            continue
        logica += linha
        instrucao, _, resto = logica.strip().partition(' ')
        instrucao = instrucao.upper()
        if instrucao not in INSTRUCOES:
            erros.append(f'linha {inicio}: instrução desconhecida "{instrucao}"')
        elif instrucao in ('ENV', 'ARG', 'LABEL'):
            erros += conferir_pares(instrucao, inicio, resto)
        logica = ''
    if logica:
        erros.append(f'linha {inicio}: continuação de linha sem fim')
    return erros


if __name__ == '__main__':
    encontrados = problemas(open(sys.argv[1], encoding='utf-8').read())
    print('\n'.join(encontrados) if encontrados else 'Dockerfile ok')
    sys.exit(1 if encontrados else 0)
