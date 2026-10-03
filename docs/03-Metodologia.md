
# Metodologia

<span style="color:red">Pré-requisitos: <a href="2-Especificação do Projeto.md"> Documentação de Especificação</a></span>

A equipe adotou uma abordagem ágil baseada no **Kanban**, com entregas organizadas em 5 etapas incrementais. O controle de versão é feito com Git e GitHub, e a comunicação da equipe ocorre via WhatsApp e reuniões presenciais durante as aulas.

## Relação de Ambientes de Trabalho

| Ambiente | Plataforma | Link de Acesso |
|----------|-----------|----------------|
| Repositório de código-fonte | GitHub | [GitHub — GestaoPro](https://github.com/ICEI-PUC-Minas-PMV-ADS/pmv-ads-2026-1-e4-infra-t1-gestao-de-condominio-provisorio) |
| Gerenciamento de projeto | GitHub Projects | [Kanban Board](https://github.com/ICEI-PUC-Minas-PMV-ADS/pmv-ads-2026-1-e4-infra-t1-gestao-de-condominio-provisorio/projects) |
| Documentação | GitHub Docs (Markdown) | Pasta `/docs` do repositório |
| IDE de desenvolvimento | Visual Studio Code / Visual Studio | Local |
| Banco de dados em nuvem | MongoDB Atlas | Painel Atlas |
| Hospedagem da API | Render | [GestaoPro API](https://render.com) |
| Build do app mobile | Expo (EAS) | Expo Dashboard |

## Controle de Versão

A ferramenta de controle de versão adotada no projeto foi o
[Git](https://git-scm.com/), sendo que o [Github](https://github.com)
foi utilizado para hospedagem do repositório.

O projeto segue a seguinte convenção para o nome de branches:

- `main`: versão estável já testada do software
- `feature/*`: branches de funcionalidades em desenvolvimento (ex.: `feature/reservas-areas`, `feature/assembly-financial`, `feature/Estacionamento-e-visitantes`)

Quanto à gerência de issues, o projeto adota a seguinte convenção para
etiquetas:

- `documentation`: melhorias ou acréscimos à documentação
- `bug`: uma funcionalidade encontra-se com problemas
- `enhancement`: uma funcionalidade precisa ser melhorada
- `feature`: uma nova funcionalidade precisa ser introduzida

O fluxo de trabalho utiliza **feature branches** com merge para `main` via **Pull Requests**. Cada membro cria sua branch a partir de `main`, desenvolve a funcionalidade e abre um PR para revisão antes do merge.

## Gerenciamento de Projeto

### Divisão de Papéis

A equipe utiliza metodologias ágeis, tendo escolhido o Kanban como base para definição do processo de desenvolvimento. A equipe está organizada da seguinte maneira:

- **Product Owner / Desenvolvedor Full Stack: ** Jefferson Luiz Mendonça da Silva
- **Scrum Master / Desenvolvedor Full Stack ** Deuslano Veloso de Oliveira Junior
- **Chief of Designer / Desenvolvedor Full Stack:** Gustavo Marques Pereira

### Processo

O grupo utiliza o recurso de gerenciamento de projeto oferecido pelo **GitHub Projects**, com quadro Kanban contendo as colunas:

- **To Do**: tarefas planejadas para a sprint/etapa atual
- **In Progress**: tarefas em andamento
- **Done**: tarefas concluídas

As tarefas são criadas como issues no repositório e movidas entre as colunas conforme o progresso. Cada Pull Request referencia as issues relacionadas.

### Ferramentas

As ferramentas empregadas no projeto são:

| Ferramenta | Finalidade |
|------------|-----------|
| **Visual Studio Code** | Edição de código (Web, Mobile, documentação) |
| **Visual Studio 2022** | Desenvolvimento da API em C# ASP.NET Core |
| **Git / GitHub** | Controle de versão e hospedagem do código |
| **GitHub Projects** | Gestão de tarefas e quadro Kanban |
| **WhatsApp** | Comunicação da equipe |
| **MongoDB Compass** | Gerenciamento visual do banco de dados |
| **Postman / Swagger** | Testes e documentação da API |
| **Expo / EAS** | Build e execução do app React Native |
| **Vite** | Servidor de desenvolvimento e build do frontend web |
| **Figma** | Wireframes e protótipos de interface |

O Visual Studio Code foi escolhido por sua integração com Git e suporte a TypeScript/React. O Visual Studio 2022 é utilizado para o backend em C# por oferecer ferramentas nativas do .NET. O Figma foi utilizado para criação dos wireframes por sua facilidade de uso colaborativo.
