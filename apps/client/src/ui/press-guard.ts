import { useCallback, useRef, useState } from "react";

/**
 * Trava de reentrância para botões que disparam uma ação assíncrona
 * (login, mutation, confirmação seguida de delete...).
 *
 * `disabled={mutation.isPending}` sozinho não resolve: o flag só chega ao botão
 * no render seguinte, e ações que abrem um diálogo antes da requisição
 * (`confirmAsync`) sequer ficam pendentes enquanto o diálogo está aberto — dois
 * toques abrem dois diálogos. A trava aqui é um ref, então já vale no segundo
 * toque, dentro do mesmo frame.
 *
 * A ação precisa devolver a promise do trabalho (use `mutateAsync`, ou um
 * handler `async`); é ela que define quando a trava é liberada. Com o
 * `mutate` fire-and-forget a trava cai imediatamente e só o `isPending` do
 * próprio site protege.
 *
 * `isRunning` acompanha a trava e serve para o estado visual do botão.
 */
export const useGuardedPress = <TArgs extends unknown[]>(
  action: (...args: TArgs) => unknown
) => {
  const isRunningRef = useRef(false);
  const [isRunning, setIsRunning] = useState(false);

  // Lido por ref para que `onPress` tenha identidade estável mesmo quando o
  // site passa uma arrow inline (o caso comum).
  const actionRef = useRef(action);
  actionRef.current = action;

  const onPress = useCallback(async (...args: TArgs) => {
    if (isRunningRef.current) {
      return;
    }

    isRunningRef.current = true;
    setIsRunning(true);

    try {
      await actionRef.current(...args);
    } finally {
      isRunningRef.current = false;
      setIsRunning(false);
    }
  }, []);

  return { onPress, isRunning };
};

/**
 * Janela em que toques repetidos no mesmo botão de navegação são ignorados.
 * Cobre a transição de tela (~350ms) com folga.
 */
const NAVIGATION_GUARD_MS = 700;

/**
 * Trava para toques que navegam (`router.push`, `router.back`).
 *
 * Navegação é síncrona: `useGuardedPress` liberaria a trava no mesmo instante e
 * o segundo toque empilharia a mesma tela de novo. Aqui a trava é por tempo —
 * ao contrário de uma trava presa ao foco da tela, não trava o botão para
 * sempre se a navegação não acontecer.
 */
export const useGuardedNavigation = <TArgs extends unknown[]>(
  navigate: (...args: TArgs) => void
) => {
  const lastPressedAtRef = useRef(0);

  const navigateRef = useRef(navigate);
  navigateRef.current = navigate;

  return useCallback((...args: TArgs) => {
    const now = Date.now();

    if (now - lastPressedAtRef.current < NAVIGATION_GUARD_MS) {
      return;
    }

    lastPressedAtRef.current = now;
    navigateRef.current(...args);
  }, []);
};
