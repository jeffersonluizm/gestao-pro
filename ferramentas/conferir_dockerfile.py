"""Conferência de sintaxe do Dockerfile sem Docker: instruções conhecidas, continuação de linha real,
ENV/ARG/LABEL só com pares chave=valor e nenhum "\\n" literal."""
import re, sys

path = sys.argv[1]
text = open(path, encoding='utf-8').read().replace('\r\n', '\n')
problems = []
if '\\n' in text:
    problems.append('"\\n" literal no arquivo')
INSTR = {'FROM', 'RUN', 'CMD', 'LABEL', 'EXPOSE', 'ENV', 'ADD', 'COPY', 'ENTRYPOINT', 'VOLUME', 'USER', 'WORKDIR', 'ARG',
         'ONBUILD', 'STOPSIGNAL', 'HEALTHCHECK', 'SHELL'}
logical, start = '', 0
for number, line in enumerate(text.split('\n'), 1):
    if not logical and (not line.strip() or line.lstrip().startswith('#')):
        continue
    if not logical:
        start = number
    if line.endswith('\\'):
        logical += line[:-1] + ' '
        continue
    logical += line
    word, _, rest = logical.strip().partition(' ')
    if word.upper() not in INSTR:
        problems.append(f'linha {start}: instrução desconhecida "{word}"')
    if word.upper() in ('ENV', 'LABEL'):
        for token in re.findall(r'(?:[^\s"]+|"[^"]*")+', rest):
            if '=' not in token:
                problems.append(f'linha {start}: "{token}" sem chave=valor em {word}')
    logical = ''
if logical:
    problems.append(f'linha {start}: continuação de linha sem fim')
print('\n'.join(problems) if problems else 'Dockerfile ok')
sys.exit(1 if problems else 0)
