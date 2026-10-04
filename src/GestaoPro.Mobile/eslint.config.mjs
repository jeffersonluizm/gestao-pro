// Lint do app: regras recomendadas de JS/TS e de hooks do React.
import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  { ignores: ['node_modules', '.expo', 'dist', 'web-build', '*.config.js'] },
  {
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    files: ['**/*.{ts,tsx}'],
    languageOptions: { ecmaVersion: 2022, globals: { ...globals.browser, ...globals.node } },
    plugins: { 'react-hooks': reactHooks },
    rules: {
      ...reactHooks.configs.recommended.rules,
      // require() é o jeito do React Native carregar imagens e fontes.
      '@typescript-eslint/no-require-imports': 'off',
    },
  },
)
