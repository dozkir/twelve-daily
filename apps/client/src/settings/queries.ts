import { authLogoutAll, usersGetProfile, usersSendRemotePushTest } from "@twelve-daily/api-client";
import { useMutation, useQuery } from "@tanstack/react-query";

import { profileKeys } from "@/src/api/query-keys";

/**
 * Estado de servidor de perfil/segurança.
 * Ações que dependem do contexto (ex.: limpar a sessão após "logout all") ficam na
 * tela, passadas como callbacks de `mutate`; aqui mora só a chamada à API.
 */
export const useProfileQuery = () =>
  useQuery({
    queryKey: profileKeys.all,
    queryFn: () => usersGetProfile()
  });

export const useLogoutAllMutation = () =>
  useMutation({
    mutationFn: () => authLogoutAll()
  });

export const useSendTestPushMutation = () =>
  useMutation({
    mutationFn: () => usersSendRemotePushTest()
  });
