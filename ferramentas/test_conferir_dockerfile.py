"""Testes da conferência de sintaxe do Dockerfile (python -m unittest discover -s ferramentas -p "test_*.py")."""
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(__file__))
from conferir_dockerfile import problemas  # noqa: E402


class ConferirDockerfileTests(unittest.TestCase):
    def test_env_com_barra_n_literal_e_recusado(self):
        texto = 'FROM base\nENV A=1 \\n    B=2\n'
        self.assertTrue(problemas(texto))

    def test_env_por_linha_e_continuacao_real_passam(self):
        texto = 'FROM base\nENV A=1\nENV B=2 \\\n    C=3\n'
        self.assertEqual([], problemas(texto))

    def test_barra_n_dentro_de_run_e_legitimo(self):
        texto = 'FROM base\nRUN echo "a\\nb" > /tmp/x\n'
        self.assertEqual([], problemas(texto))

    def test_barra_n_entre_aspas_no_env_e_legitimo(self):
        texto = 'FROM base\nENV MENSAGEM="linha1\\nlinha2"\n'
        self.assertEqual([], problemas(texto))

    def test_forma_antiga_env_chave_valor_com_espaco(self):
        texto = 'FROM base\nENV A 1\nENV CAMINHO /usr/local/bin\n'
        self.assertEqual([], problemas(texto))

    def test_instrucao_desconhecida_e_recusada(self):
        self.assertTrue(problemas('FROM base\nRODAR echo oi\n'))

    def test_continuacao_sem_fim_e_recusada(self):
        # Arquivo que termina logo depois da barra de continuação.
        self.assertTrue(problemas('FROM base\nRUN echo a \\'))


if __name__ == '__main__':
    unittest.main()
