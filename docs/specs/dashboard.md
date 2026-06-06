# Spec — Dashboard Semanal

> ⚠️ Rascunho — detalhes a refinar durante o desenvolvimento.

## Visão Geral
Tela acessada pelo menu hamburger. Apresenta um resumo do desempenho do usuário na semana atual (ou semana selecionada).

---

## Denominador

O denominador de um dia é a quantidade de hábitos que **deveriam contar** naquele dia: existiam (`Habit.CreatedAt` ≤ data) e tinham `HabitSchedule` ativo para o `DayOfWeek`, **independente** do estado de ativação atual (um hábito inativado hoje continua contando nos dias passados). Dias futuros ainda não contam. O numerador são os hábitos com `HabitCheck` naquele dia.

## Métricas

| Métrica | Descrição |
|---|---|
| Taxa de conclusão por dia | % de hábitos concluídos sobre os esperados em cada dia |
| Total esperado | Soma dos hábitos esperados na semana (`total`) |
| Total concluído | Soma dos hábitos com check na semana (`completed`) |
| Streak atual | *(proposta)* dias consecutivos com 100% de conclusão |
| Melhor / pior hábito | *(proposta)* maior/menor taxa no período |

---

## Endpoint (implementado)

```
GET /dashboard/weekly?weekStart=2026-03-23

Response 200:
{
  total: 24,
  completed: 18,
  completionRate: 75,          // 0–100, já em pontos percentuais
  dayByDay: [
    { date: "2026-03-23", total: 4, completed: 4 },
    { date: "2026-03-24", total: 3, completed: 2 },
    ...
  ]
}
```

> Métricas como streak e melhor/pior hábito ainda são propostas; a resposta atual cobre `total`, `completed`, `completionRate` e `dayByDay`.

---

## Interface (proposta inicial)

- **Heatmap / barras** de conclusão por dia (Seg → Dom)
- Destaque visual para o streak
- Cards para melhor e pior hábito
- Seletor de semana para navegar semanas anteriores

> Tudo aqui está sujeito a ajuste quando formos implementar a tela.

