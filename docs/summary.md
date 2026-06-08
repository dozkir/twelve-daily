# Twelve Daily

## O que é
Twelve Daily é um aplicativo de **rastreamento de hábitos diários** que ajuda o usuário a organizar e acompanhar sua rotina. Cada dia da semana pode ter hábitos diferentes, cada um com horário de início e fim definidos pelo próprio usuário.

## Como funciona
O usuário cadastra seus hábitos e define em quais dias da semana cada um ocorre, com horários específicos. Na madrugada de cada dia, o sistema gera automaticamente a lista de hábitos programados — a **rotina do dia**. Ao longo do dia, o usuário marca cada hábito como concluído conforme os realiza.

A tela principal é uma **timeline vertical** que acompanha o horário atual em tempo real. Cada hábito aparece como um bloco proporcional à sua duração, com indicação visual clara:
- 🟢 **Verde** — concluído
- 🔴 **Vermelho** — atrasado
- ⚪ **Neutro** — pendente

Notificações push lembram o usuário no horário de cada hábito. Um dashboard semanal mostra o desempenho geral — taxa de conclusão, sequência de dias completos e destaques.

## Plataformas
- **Web** (browser)
- **iOS**
- **Android**

Todas as plataformas compartilham a mesma base de código (Expo/React Native) e sincronizam em tempo real — marcar um hábito no celular reflete instantaneamente no browser.

## Tecnologias principais
- **Backend**: .NET 10 / C# 13, PostgreSQL, SignalR
- **Frontend**: Expo (React Native + Web), TypeScript
- **Infraestrutura**: Docker, Fly.io, GitHub Actions

## Documentação
A especificação completa do projeto está em [`docs/index.md`](docs/index.md).

