# Twelve Daily

## O que é
Twelve Daily é um aplicativo de **rastreamento de hábitos diários** que ajuda o usuário a organizar e acompanhar sua rotina. Cada dia da semana pode ter hábitos diferentes, cada um com horário de início e fim definidos pelo próprio usuário.

## Como funciona
O usuário cadastra seus hábitos e define em quais dias da semana cada um ocorre, com horários específicos. A **rotina do dia** não é materializada: a lista de hábitos programados é reconstruída sob demanda a partir do hábito, do seu schedule e dos checks já registrados — abrir a tela não cria dados no banco. Ao longo do dia, o usuário marca cada hábito como concluído conforme os realiza.

A tela principal é uma **timeline vertical** que acompanha o horário atual em tempo real. Cada hábito aparece como um bloco proporcional à sua duração, com indicação visual clara:
- 🟢 **Verde** — concluído
- 🔴 **Vermelho** — atrasado
- ⚪ **Neutro** — pendente

Notificações push lembram o usuário no horário de cada hábito. Um dashboard semanal mostra o desempenho geral — taxa de conclusão, sequência de dias completos e destaques.

## Plataformas
- **Web** (browser)
- **iOS**
- **Android**

Todas as plataformas compartilham a mesma base de código (Expo/React Native). A sincronização em tempo real entre dispositivos (marcar um hábito no celular refletir instantaneamente no browser, via SignalR) é **planejada — ainda não implementada**.

## Tecnologias principais
- **Backend**: .NET 10 / C# 13, PostgreSQL, Hangfire (jobs/push) *(SignalR planejado)*
- **Frontend**: Expo (React Native + Web), TypeScript
- **Infraestrutura**: Docker, Fly.io, GitHub Actions

## Documentação
A especificação completa do projeto está em [`docs/index.md`](docs/index.md).

